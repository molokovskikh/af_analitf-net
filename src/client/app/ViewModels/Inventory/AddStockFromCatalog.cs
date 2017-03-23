using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using NHibernate;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class AddStockFromCatalog : BaseScreen, ICancelable
	{

		private Producer emptyProducer = new Producer(Consts.AllProducerLabel);
		private ISession _session;

		public AddStockFromCatalog(ISession session, Address address)
		{
			DisplayName = "Добавление из каталога";
			Item = new Stock() {
				Status = StockStatus.Available,
				Quantity = 1,
				ReservedQuantity = 0,
				SupplyQuantity = 1,
				Address=address
			};

			CurrentCatalog = new NotifyValue<Catalog>();
			CatalogTerm = new NotifyValue<string>();

			ProducerTerm = new NotifyValue<string>();
			CurrentProducer = new NotifyValue<Producer>(emptyProducer);
			WasCancelled = true;
			_session = session;
		}

		public bool WasCancelled { get; private set; }
		public Stock Item { get; set; }
		public NotifyValue<List<Catalog>> Catalogs { get; set; }
		public NotifyValue<Catalog> CurrentCatalog { get; set; }
		public NotifyValue<string> CatalogTerm { get; set; }
		public NotifyValue<bool> IsCatalogOpen { get; set; }

		public NotifyValue<List<Producer>> Producers { get; set; }
		public NotifyValue<bool> IsProducerOpen { get; set; }
		public NotifyValue<Producer> CurrentProducer { get; set; }
		public NotifyValue<string> ProducerTerm { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Catalogs = CatalogTerm
				.Throttle(Consts.TextInputLoadTimeout, Scheduler)
				.Select(t => RxQuery(s => {
					// при threshold = 1 возвращается около 88 тыс строк на ввод "а"
					var threshold = 2;
					if (String.IsNullOrEmpty(t) || t.Length < threshold)
						return new List<Catalog>();
					if (CurrentCatalog.Value != null && CurrentCatalog.Value.FullName == t) {
						return Catalogs.Value;
					}
					// union distinct работает с полностью одинаковыми строками, здесь они разные из-за поля Score
					return s.CreateSQLQuery(@"
(select {c.*}, 0 as Score
from Catalogs c
where c.Fullname like :term)
union
(select {c.*}, 1 as Score
from Catalogs c
where c.Fullname like :fullterm and c.Fullname not like :term)
order by Score, {c.FullName}")
						.AddEntity("c", typeof(Catalog))
						.SetParameter("term", t + "%")
						.SetParameter("fullterm", "%" + t + "%")
						.List<Catalog>()
						.ToList();
				}))
				.Switch()
				.ToValue(CloseCancellation);
			IsCatalogOpen = Catalogs.Select(v => v != null && v.Count > 0).Where(v => v).ToValue();
			CurrentCatalog.Subscribe(v => {
				Item.CatalogId =(v != null && v.Id > 0) ? v.Id : (uint?)null;
				Item.Product = (v != null && v.Id > 0) ? v.FullName : string.Empty;
			});

			Producers = ProducerTerm
				.Throttle(Consts.TextInputLoadTimeout, Scheduler)
				.Select(t => RxQuery(s => {
					if (String.IsNullOrEmpty(t))
						return new List<Producer> {emptyProducer};
					if (CurrentProducer.Value != null && CurrentProducer.Value.Name == t)
						return Producers.Value;

					CurrentProducer.Value = null;
					var items = s.CreateSQLQuery(@"
(select {p.*}, 0 as Score
from Producers p
where p.Name like :term)
union
(select {p.*}, 1 as Score
from Producers p
where p.Name like :fullterm and p.Name not like :term)
order by Score, {p.Name}")
						.AddEntity("p", typeof(Producer))
						.SetParameter("term", t + "%")
						.SetParameter("fullterm", "%" + t + "%")
						.List<Producer>();
					return new[] {emptyProducer}.Concat(items).ToList();
				}))
				.Switch()
				.ToValue(CloseCancellation);
			IsProducerOpen = Producers.Select(v => v != null && v.Count > 1).Where(v => v).ToValue();
			CurrentProducer.Subscribe(v => {
				Item.Producer = (v != null && v.Id > 0) ? v.Name : string.Empty;
				Item.ProducerId=(v != null && v.Id > 0) ? v.Id : (uint?)null;
			});
		}

		public void OK()
		{
			WasCancelled = false;
			TryClose();
		}
	}
}

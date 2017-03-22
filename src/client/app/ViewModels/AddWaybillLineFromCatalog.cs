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

namespace AnalitF.Net.Client.ViewModels
{
	public class AddWaybillLineFromCatalog : BaseScreen, ICancelable
	{
		public AddWaybillLineFromCatalog()
		{
			InitFields();
			DisplayName = "Добавление из каталога";
			CurrentCatalog = new NotifyValue<Catalog>();
			CatalogTerm = new NotifyValue<string>();
			ProducerTerm = new NotifyValue<string>();
			CurrentProducer = new NotifyValue<Producer>();
			WasCancelled = true;
			IsOkEnabled.Value = false;
			CurrentCatalog.Changed()
				.Merge(CurrentProducer.Changed())
				.Merge(SupplierCost.Changed())
				.Merge(Quantity.Changed())
				.Subscribe(_ => IsOkEnabled.Value = CurrentCatalog.Value != null && CurrentProducer.Value != null && SupplierCost.Value > 0 && Quantity.Value > 0);
		}

		public bool WasCancelled { get; private set; }
		public NotifyValue<List<Catalog>> Catalogs { get; set; }
		public NotifyValue<Catalog> CurrentCatalog { get; set; }
		public NotifyValue<string> CatalogTerm { get; set; }
		public NotifyValue<bool> IsCatalogOpen { get; set; }

		public NotifyValue<List<Producer>> Producers { get; set; }
		public NotifyValue<bool> IsProducerOpen { get; set; }
		public NotifyValue<Producer> CurrentProducer { get; set; }
		public NotifyValue<string> ProducerTerm { get; set; }

		public NotifyValue<decimal> SupplierCost { get; set; }
		public NotifyValue<int> Quantity { get; set; }

		public NotifyValue<bool> IsOkEnabled { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Catalogs = CatalogTerm
				.Throttle(Consts.TextInputLoadTimeout, Scheduler)
				.Select(t => RxQuery(s => {
					if (String.IsNullOrEmpty(t))
						return new List<Catalog>();
					if (CurrentCatalog.Value != null && CurrentCatalog.Value.FullName == t) {
						return Catalogs.Value;
					}

					return s.CreateSQLQuery(@"
(select {c.*}, 0 as Score
from Catalogs c
where c.Fullname like :term)
union distinct
(select {c.*}, 1 as Score
from Catalogs c
where c.Fullname like :fullterm)
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

			Producers = ProducerTerm
				.Throttle(Consts.TextInputLoadTimeout, Scheduler)
				.Select(t => RxQuery(s => {
					if (String.IsNullOrEmpty(t))
						return new List<Producer>();
					if (CurrentProducer.Value != null && CurrentProducer.Value.Name == t)
						return Producers.Value;

					CurrentProducer.Value = null;
					return s.CreateSQLQuery(@"
(select {p.*}, 0 as Score
from Producers p
where p.Name like :term)
union distinct
(select {p.*}, 1 as Score
from Producers p
where p.Name like :fullterm)
order by Score, {p.Name}")
						.AddEntity("p", typeof(Producer))
						.SetParameter("term", t + "%")
						.SetParameter("fullterm", "%" + t + "%")
						.List<Producer>()
						.ToList();
				}))
				.Switch()
				.ToValue(CloseCancellation);
			IsProducerOpen = Producers.Select(v => v != null && v.Count > 1).Where(v => v).ToValue();
		}

		public void OK()
		{
			WasCancelled = false;
			TryClose();
		}
	}
}

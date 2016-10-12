using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;

namespace AnalitF.Net.Client.ViewModels.Offers
{
	public class AddAwaited : BaseScreen, ICancelable
	{
		private Producer emptyProducer = new Producer(Consts.AllProducerLabel);

		public AddAwaited()
		{
			DisplayName = "Добавление ожидаемой позиции";
			Item = new AwaitedItem();

			CurrentCatalog = new NotifyValue<Catalog>();
			CatalogTerm = new NotifyValue<string>();

			ProducerTerm = new NotifyValue<string>();
			CurrentProducer = new NotifyValue<Producer>(emptyProducer);
			WasCancelled = true;
		}

		public bool WasCancelled { get; private set; }
		public AwaitedItem Item { get; set; }
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
				.ObserveOn(UiScheduler)
				.ToValue(CloseCancellation);
			IsCatalogOpen = Catalogs.Select(v => v != null && v.Count > 0).Where(v => v).ToValue();
			CurrentCatalog.Subscribe(v => Item.Catalog = v);

			Producers = ProducerTerm
				.Throttle(Consts.TextInputLoadTimeout, Scheduler)
				.Select(t => RxQuery(s => {
					if (String.IsNullOrEmpty(t))
						return new List<Producer> { emptyProducer };
					if (CurrentProducer.Value != null && CurrentProducer.Value.Name == t)
						return Producers.Value;

					CurrentProducer.Value = null;
					var items = s.CreateSQLQuery(@"
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
						.List<Producer>();
					return new[] { emptyProducer }.Concat(items).ToList();
				}))
				.Switch()
				.ObserveOn(UiScheduler)
				.ToValue(CloseCancellation);
			IsProducerOpen = Producers.Select(v => v != null && v.Count > 1).Where(v => v).ToValue();
			CurrentProducer.Subscribe(v => Item.Producer = (v != null && v.Id > 0) ? v : null);
		}

		public void OK()
		{
			var error = Env.Query(s => Item.TrySave(s)).Result;
			if (String.IsNullOrEmpty(error)) {
				WasCancelled = false;
				TryClose();
			} else {
				Manager.Warning(error);
			}
		}
	}
}
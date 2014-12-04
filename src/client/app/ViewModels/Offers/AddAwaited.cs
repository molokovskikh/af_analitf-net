﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Newtonsoft.Json.Utilities;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Offers
{
	public class AddAwaited : BaseScreen
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
		}

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

			//вообще то это грабли тк StatelessSession формально не производит внутренней синхронизации
			//но судя по реализации в данном случает все будет хорошо
			//тк для каждого вызова создается свое соединение и свой контекст
			//однако я могу и ошибаться
			//в любом случае функция не критическая
			Catalogs = CatalogTerm
				.Throttle(Consts.TextInputLoadTimeout, Scheduler)
				.Select(t => {
					if (String.IsNullOrEmpty(t))
						return Observable.Return(new List<Catalog>());
					if (CurrentCatalog.Value != null && CurrentCatalog.Value.FullName == t) {
						return Observable.Return(Catalogs.Value);
					}

					var items = StatelessSession.CreateSQLQuery(@"
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
					return Observable.Return(items);
				})
				.Switch()
				.ObserveOn(UiScheduler)
				.ToValue(CloseCancellation);
			IsCatalogOpen = Catalogs.Select(v => v != null && v.Count > 0).Where(v => v).ToValue();
			CurrentCatalog.Subscribe(v => Item.Catalog = v);

			Producers = ProducerTerm
				.Throttle(Consts.TextInputLoadTimeout, Scheduler)
				.Select(t => {
					if (String.IsNullOrEmpty(t))
						return Observable.Return(new List<Producer> { emptyProducer });
					if (CurrentProducer.Value != null && CurrentProducer.Value.Name == t)
						return Observable.Return(Producers.Value);

					CurrentProducer.Value = null;
					var items = StatelessSession.CreateSQLQuery(@"
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
					return Observable.Return(new[] { emptyProducer }.Concat(items).ToList());
				})
				.Switch()
				.ObserveOn(UiScheduler)
				.ToValue(CloseCancellation);
			IsProducerOpen = Producers.Select(v => v != null && v.Count > 1).Where(v => v).ToValue();
			CurrentProducer.Subscribe(v => Item.Producer = (v != null && v.Id > 0) ? v : null);
		}

		public void OK()
		{
			var message = "";
			if (Item.TrySave(StatelessSession, out message)) {
				TryClose(true);
			}
			else {
				Manager.Warning(message);
			}
		}
	}
}
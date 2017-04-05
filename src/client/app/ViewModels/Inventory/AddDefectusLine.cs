using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class AddDefectusLine : BaseScreen2, ICancelable
	{
		public AddDefectusLine()
		{
			DisplayName = "Добавление из каталога";
			Item = new DefectusLine() {
				Threshold	= 0,
				OrderQuantity = 0,
			};
			WasCancelled = true;
		}

		public bool WasCancelled { get; private set; }
		public DefectusLine Item { get; set; }
		public NotifyValue<List<Product>> Catalogs { get; set; }
		public NotifyValue<Product> CurrentCatalog { get; set; }
		public NotifyValue<string> CatalogTerm { get; set; }
		public NotifyValue<bool> IsCatalogOpen { get; set; }

		public NotifyValue<List<Producer>> Producers { get; set; }
		public NotifyValue<bool> IsProducerOpen { get; set; }
		public NotifyValue<Producer> CurrentProducer { get; set; }
		public NotifyValue<string> ProducerTerm { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			CatalogTerm
				.Throttle(Consts.TextInputLoadTimeout, Scheduler)
				.SelectMany(t => RxQuery(s => {
					var threshold = 2;
					if (String.IsNullOrEmpty(t) || t.Length < threshold)
						return new List<Product>();
					if (CurrentCatalog.Value != null && CurrentCatalog.Value.Name == t) {
						return Catalogs.Value;
					}
					return s.CreateSQLQuery(@"
(select {p.*}, 0 as Score
from Products p
where p.Name like :term)
union
(select {p.*}, 1 as Score
from Products p
where p.Name like :fullterm and p.Name not like :term)
order by Score, {p.Name}")
						.AddEntity("p", typeof(Product))
						.SetParameter("term", t + "%")
						.SetParameter("fullterm", "%" + t + "%")
						.List<Product>()
						.ToList();
				}))
				.Subscribe(Catalogs, CloseCancellation.Token);

			Catalogs.Subscribe(x => IsCatalogOpen.Value = x != null && x.Count > 0);

			CurrentCatalog.Subscribe(v => {
				Item.ProductId = (v != null && v.Id > 0) ? v.Id : 0;
				Item.CatalogId = (v != null && v.CatalogId > 0) ? v.CatalogId : 0;
				Item.Product = (v != null && v.Id > 0) ? v.Name : string.Empty;
			});

			ProducerTerm
				.Throttle(Consts.TextInputLoadTimeout, Scheduler)
				.Do(t => {
					if (String.IsNullOrEmpty(t))
						CurrentProducer.Value = null;
				})
				.SelectMany(t => RxQuery(s => {
					if (String.IsNullOrEmpty(t))
						return s.Query<Producer>().OrderBy(x => x.Name).ToList();
					if (CurrentProducer.Value != null && CurrentProducer.Value.Name == t)
						return Producers.Value;
					return s.CreateSQLQuery(@"
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
						.List<Producer>()
						.ToList();
				}))
				.Subscribe(Producers, CloseCancellation.Token);

			Producers.Subscribe(x => IsProducerOpen.Value = x != null && x.Count > 0);

			CurrentProducer.Subscribe(v => {
				Item.Producer = (v != null && v.Id > 0) ? v.Name : string.Empty;
				Item.ProducerId = (v != null && v.Id > 0) ? v.Id : (uint?)null;
			});
		}

		public void OK()
		{
			WasCancelled = false;
			TryClose();
		}
	}
}

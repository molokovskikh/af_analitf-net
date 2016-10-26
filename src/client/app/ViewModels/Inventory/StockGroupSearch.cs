using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using System;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class StockGroupSearch : BaseScreen2, ICancelable
	{
		public StockGroupSearch()
		{
			DisplayName = "Поиск группы товаров";
			WasCancelled = true;
			DateBegin.Value = DateTime.Today.AddDays(-7);
			DateEnd.Value = DateTime.Today;
			SupplierCostBegin.Value = 0;
			SupplierCostEnd.Value = 1000;
			RetailCostBegin.Value = 0;
			RetailCostEnd.Value = 1000;
		}

		public NotifyValue<DateTime> DateBegin { get; set; }
		public NotifyValue<DateTime> DateEnd { get; set; }
		public NotifyValue<decimal> SupplierCostBegin { get; set; }
		public NotifyValue<decimal> SupplierCostEnd { get; set; }
		public NotifyValue<decimal> RetailCostBegin { get; set; }
		public NotifyValue<decimal> RetailCostEnd { get; set; }
		public bool WasCancelled { get; private set; }
		public NotifyValue<List<Stock>> Items { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			DbReloadToken
				.Merge(DateBegin.Changed())
				.Merge(DateEnd.Changed())
				.Merge(SupplierCostBegin.Changed())
				.Merge(SupplierCostEnd.Changed())
				.Merge(RetailCostBegin.Changed())
				.Merge(RetailCostEnd.Changed())
				.SelectMany(_ => RxQuery(LoadItems))
				.Subscribe(Items);
		}

		public List<Stock> LoadItems(IStatelessSession session)
		{
			return Stock.AvailableStocks(session, Address)
				.Where(x => x.DocumentDate > DateBegin.Value
					&& x.DocumentDate < DateEnd.Value.AddDays(1)
					&& x.SupplierCost >= SupplierCostBegin.Value
					&& x.SupplierCost <= SupplierCostEnd.Value
					&& x.RetailCost >= RetailCostBegin.Value
					&& x.RetailCost <= RetailCostEnd.Value)
				.OrderBy(x => x.Product)
				.ThenBy(x => x.RetailCost)
				.ToList();
		}

		public void OK()
		{
			WasCancelled = false;
			TryClose();
		}

		public void Close()
		{
			TryClose();
		}
	}
}
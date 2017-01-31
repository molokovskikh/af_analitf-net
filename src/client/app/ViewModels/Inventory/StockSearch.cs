using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Parts;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public enum SearchType
	{
		[Description("По наименование")] ByName,
		[Description("По цене")] ByCost,
		[Description("По поставщику")] BySupplier,
	}

	public class StockSearch : BaseScreen2, ICancelable
	{
		public StockSearch(string term = "")
		{
			DisplayName = "Поиск товара по названию";
			SearchBehavior = new SearchBehavior(this);
			SearchBehavior.ActiveSearchTerm.Value = term;
			SearchBehavior.SearchText.Value = term;
			WasCancelled = true;
			Type = SearchType.ByName;
		}

		public StockSearch(decimal cost)
		{
			DisplayName = "Поиск товара по цене";
			SearchBehavior = new SearchBehavior(this);
			SearchBehavior.ActiveSearchTerm.Value = cost.ToString();
			SearchBehavior.SearchText.Value = cost.ToString();
			WasCancelled = true;
			Type = SearchType.ByCost;
		}

		public StockSearch(uint id)
		{
			DisplayName = "Поиск товара по поставщику";
			SearchBehavior = new SearchBehavior(this);
			SearchBehavior.ActiveSearchTerm.Value = id.ToString();
			SearchBehavior.SearchText.Value = id.ToString();
			WasCancelled = true;
			Type = SearchType.BySupplier;
		}

		public bool WasCancelled { get; private set; }
		public SearchBehavior SearchBehavior { get; set; }
		public NotifyValue<Stock> CurrentItem { get; set; }
		public NotifyValue<List<Stock>> Items { get; set; }
		public SearchType Type { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			if (Type == SearchType.ByName)
				SearchByName();

			if(Type == SearchType.ByCost)
				SearhByCost();

			if (Type == SearchType.BySupplier)
				SearhBySupplier();
		}

		private void SearchByName()
		{
			SearchBehavior.ActiveSearchTerm.Throttle(Consts.TextInputLoadTimeout, UiScheduler)
				.SelectMany(_ => RxQuery(s => Stock.AvailableStocks(s, Address).Where(x => x.Product.Contains(SearchBehavior.ActiveSearchTerm.Value ?? ""))
					.OrderBy(x => x.Product)
					.ThenBy(x => x.RetailCost)
					.ToList()))
				.Subscribe(Items);
		}

		private void SearhByCost()
		{
			SearchBehavior.ActiveSearchTerm.Throttle(Consts.TextInputLoadTimeout, UiScheduler)
				.SelectMany(_ => RxQuery(s => Stock.AvailableStocks(s, Address).Where(x => x.RetailCost == Convert.ToDecimal(SearchBehavior.ActiveSearchTerm.Value))
					.OrderBy(x => x.Product)
					.ThenBy(x => x.RetailCost)
					.ToList()))
				.Subscribe(Items);
		}

		private void SearhBySupplier()
		{
			SearchBehavior.ActiveSearchTerm.Throttle(Consts.TextInputLoadTimeout, UiScheduler)
				.SelectMany(_ => RxQuery(s => Stock.AvailableStocks(s, Address).Where(x => x.SupplierId == Convert.ToUInt32(SearchBehavior.ActiveSearchTerm.Value))
					.OrderBy(x => x.Product)
					.ThenBy(x => x.RetailCost)
					.ToList()))
				.Subscribe(Items);
		}

		public void EnterItem()
		{
			if (CurrentItem.Value == null)
				return;
			WasCancelled = false;
			TryClose();
		}
	}
}
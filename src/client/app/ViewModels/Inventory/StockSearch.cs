using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Parts;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class StockSearch : BaseScreen2, ICancelable
	{
		public StockSearch(string term = "")
		{
			DisplayName = "Поиск товара";
			SearchBehavior = new SearchBehavior(this);
			SearchBehavior.ActiveSearchTerm.Value = term;
			WasCancelled = true;
		}

		public bool WasCancelled { get; private set; }
		public SearchBehavior SearchBehavior { get; set; }
		public NotifyValue<Stock> CurrentItem { get; set; }
		public NotifyValue<List<Stock>> Items { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			SearchBehavior.ActiveSearchTerm.Throttle(Consts.TextInputLoadTimeout, UiScheduler)
				.SelectMany(_ => RxQuery(s => Stock.AvailableStocks(s, Address).Where(x => x.Product.Contains(SearchBehavior.ActiveSearchTerm.Value ?? ""))
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
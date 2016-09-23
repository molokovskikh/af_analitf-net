using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Parts;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class ProductSearch : BaseScreen2, ICancelable
	{
		public ProductSearch()
		{
			DisplayName = "Поиск товара";
			SearchBehavior = new SearchBehavior(this);
			WasCancelled = true;
		}

		public bool WasCancelled { get; private set; }
		public SearchBehavior SearchBehavior { get; set; }
		public NotifyValue<Catalog> CurrentItem { get; set; }
		public NotifyValue<List<Catalog>> Items { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			SearchBehavior.ActiveSearchTerm.Throttle(Consts.TextInputLoadTimeout, UiScheduler)
				.SelectMany(_ => RxQuery(s => s.Query<Catalog>().Where(x => x.FullName.Contains(SearchBehavior.ActiveSearchTerm.Value ?? ""))
					.OrderBy(x => x.FullName)
					.Take(100)
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
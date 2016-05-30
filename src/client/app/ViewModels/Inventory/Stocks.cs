using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class Stocks : BaseScreen
	{
		public Stocks()
		{
			InitFields();
		}

		public NotifyValue<List<Stock>> Items { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			RxQuery(x => x.Query<Stock>().ToList())
				.ObserveOn(UiScheduler)
				.Subscribe(Items);
		}
	}
}
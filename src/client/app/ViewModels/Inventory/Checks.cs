using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	class Checks : BaseScreen2
	{
		private Main main;

		public Checks()
		{
			Begin.Value = DateTime.Today.AddDays(-7);
			End.Value = DateTime.Today;
		}

		public Checks(Main main)
			: this()
		{
			this.main = main;
		}

		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		public NotifyValue<List<Check>> Items { get; set; }
		public NotifyValue<Check> CurrentItem { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			RxQuery(x => x.Query<Check>()
					.OrderByDescending(y => y.Date).ToList())
				.Subscribe(Items);
		}
		public void EnterItem()
		{
			if (CurrentItem.Value == null)
				return;
			main.ActiveItem = new CheckDetails(CurrentItem.Value.Id);
		}
	}
}

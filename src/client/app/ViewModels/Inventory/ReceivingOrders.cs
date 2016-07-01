using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class ReceivingOrders : BaseScreen2
	{
		private Main main;

		public ReceivingOrders(Main main)
		{
			this.main = main;
		}

		public NotifyValue<List<ReceivingOrder>> Items { get; set; }
		public NotifyValue<ReceivingOrder> CurrentItem { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			RxQuery(x => x.Query<ReceivingOrder>().Fetch(y => y.Supplier).OrderByDescending(y => y.OrderDate).ToList())
				.Subscribe(Items);
		}

		public void Create()
		{
			throw new NotImplementedException();
		}

		public void EnterItem()
		{
			if (CurrentItem.Value == null)
				return;
			main.ActiveItem = new ReceivingDetails(CurrentItem.Value.Id);
		}
	}
}
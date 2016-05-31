using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class ReceivingDetails : BaseScreen2
	{
		private uint id;

		public ReceivingDetails(uint id)
		{
			this.id = id;
			DisplayName = "Заказ на приемку";
		}

		public NotifyValue<ReceivingOrder> Header { get; set; }
		public NotifyValue<List<Stock>> Items { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			RxQuery(x => x.Query<ReceivingOrder>().Fetch(y => y.Supplier).FirstOrDefault(y => y.Id == id))
				.Subscribe(Header);
			RxQuery(x => x.Query<Stock>().Where(y => y.ReceivingOrderId == id).OrderBy(y => y.Product).ToList())
				.Subscribe(Items);
		}
	}
}
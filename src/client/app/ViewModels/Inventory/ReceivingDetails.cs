using System;
using System.Collections.ObjectModel;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class ReceivingDetails : BaseScreen2
	{
		private uint id;

		public ReceivingDetails()
		{
			DisplayName = "Заказ на приемку";
		}

		public ReceivingDetails(uint id)
			: this()
		{
			this.id = id;
		}

		public NotifyValue<ReceivingOrder> Header { get; set; }
		public NotifyValue<ReceivingLine> CurrentLine { get; set; }
		public NotifyValue<ObservableCollection<ReceivingLine>> Lines { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			if (Header.Value == null) {
				RxQuery(x => x.Query<ReceivingOrder>()
						.Fetch(y => y.Supplier)
						.Fetch(y => y.Address)
						.FirstOrDefault(y => y.Id == id))
					.Subscribe(Header);
				RxQuery(x => {
						return x.Query<ReceivingLine>().Where(y => y.ReceivingOrderId == id).OrderBy(y => y.Product)
							.ToList()
							.ToObservableCollection();
					})
					.Subscribe(Lines);
			}
		}
	}
}
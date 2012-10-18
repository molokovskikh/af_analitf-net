using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class OrdersViewModel : BaseScreen
	{
		private Order currentOrder;

		public OrdersViewModel()
		{
			DisplayName = "Заказы";
			Orders = Session.Query<Order>().OrderBy(o => o.CreatedOn).ToList();
		}

		public List<Order> Orders { get; set; }

		public Order CurrentOrder
		{
			get { return currentOrder; }
			set
			{
				currentOrder = value;
				RaisePropertyChangedEventImmediately("CurrentOrder");
			}
		}

		public void EnterOrder()
		{
			if (CurrentOrder == null)
				return;

			Shell.ActivateItem(new OrderDetailsViewModel(CurrentOrder));
		}

		protected override void OnDeactivate(bool close)
		{
			Session.Flush();
			base.OnDeactivate(close);
		}
	}
}
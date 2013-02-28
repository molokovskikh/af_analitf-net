using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using AnalitF.Net.Client.Models;
using Common.Tools.Calendar;
using NHibernate.Linq;
using ReactiveUI;
using log4net;

namespace AnalitF.Net.Client.ViewModels
{
	public class OrdersViewModel : BaseOrderViewModel
	{
		private Order currentOrder;
		private List<Order> orders;
		private List<SentOrder> sentOrders;

		public OrdersViewModel()
		{
			DisplayName = "Заказы";

			Begin = DateTime.Today.AddMonths(-3).FirstDayOfMonth();
			End = DateTime.Today;
			Orders = Session.Query<Order>().OrderBy(o => o.CreatedOn).ToList();
		}

		public override void Update()
		{
			if (IsSentSelected)
				SentOrders = StatelessSession.Query<SentOrder>()
					.Where(o => o.SentOn >= Begin && o.SentOn < End.AddDays(1))
					.Fetch(o => o.Price)
					.OrderBy(o => o.SentOn)
					.Take(1000)
					.ToList();
		}

		public List<Order> Orders
		{
			get { return orders; }
			set
			{
				orders = value;
				NotifyOfPropertyChange("Orders");
			}
		}

		public List<SentOrder> SentOrders
		{
			get { return sentOrders; }
			set
			{
				sentOrders = value;
				NotifyOfPropertyChange("SentOrders");
			}
		}

		public Order CurrentOrder
		{
			get { return currentOrder; }
			set
			{
				currentOrder = value;
				NotifyOfPropertyChange("CurrentOrder");
			}
		}

		public bool CanDelete
		{
			get { return CurrentOrder != null && !IsSentSelected; }
		}

		public void Delete()
		{
			if (!CanDelete)
				return;
			Session.Delete(CurrentOrder);
			Orders.Remove(CurrentOrder);
		}

		public void EnterOrder()
		{
			if (CurrentOrder == null)
				return;

			Shell.Navigate(new OrderDetailsViewModel(CurrentOrder));
		}
	}
}
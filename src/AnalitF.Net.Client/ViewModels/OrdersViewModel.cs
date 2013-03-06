using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using Common.Tools.Calendar;
using NHibernate.Linq;
using ReactiveUI;
using log4net;

namespace AnalitF.Net.Client.ViewModels
{
	public class OrdersViewModel : BaseOrderViewModel, IPrintable
	{
		private Order currentOrder;
		private BindingList<Order> orders;
		private List<SentOrder> sentOrders;

		public OrdersViewModel()
		{
			DisplayName = "Заказы";

			Begin = DateTime.Today.AddMonths(-3).FirstDayOfMonth();
			End = DateTime.Today;

			this.ObservableForProperty(m => (object)m.CurrentOrder)
				.Merge(this.ObservableForProperty(m => (object)m.IsCurrentSelected))
				.Subscribe(_ => {
					NotifyOfPropertyChange("CanDelete");
					NotifyOfPropertyChange("CanFreeze");
					NotifyOfPropertyChange("CanUnfreeze");
				});
			this.ObservableForProperty(m => m.CurrentOrder.Frozen)
				.Subscribe(_ => {
					NotifyOfPropertyChange("CanFreeze");
					NotifyOfPropertyChange("CanUnfreeze");
				});
		}

		protected override void OnActivate()
		{
			base.OnActivate();

			Update();
		}

		public override void Update()
		{
			if (IsSentSelected) {
				SentOrders = StatelessSession.Query<SentOrder>()
					.Where(o => o.SentOn >= Begin && o.SentOn < End.AddDays(1))
					.Fetch(o => o.Price)
					.OrderBy(o => o.SentOn)
					.Take(1000)
					.ToList();
			}
			else {
				Orders = new BindingList<Order>(Session.Query<Order>().OrderBy(o => o.CreatedOn).ToList());
				var observable = Observable.FromEventPattern<ListChangedEventArgs>(Orders, "ListChanged")
					.Throttle(Consts.RefreshOrderStatTimeout, UiScheduler)
					.Select(e => new Stat(Address));
				Bus.RegisterMessageSource(observable);
			}
		}

		[Export]
		public BindingList<Order> Orders
		{
			get { return orders; }
			set
			{
				orders = value;
				NotifyOfPropertyChange("Orders");
			}
		}

		[Export]
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
			DeleteOrder(CurrentOrder);
		}

		private void DeleteOrder(Order order)
		{
			Session.Delete(order);
			Orders.Remove(order);
		}

		public bool CanFreeze
		{
			get { return CurrentOrder != null && !CurrentOrder.Frozen && !IsSentSelected; }
		}

		public void Freeze()
		{
			if (!CanFreeze)
				return;

			CurrentOrder.Frozen = true;
			CurrentOrder.Send = false;
		}

		public bool CanUnfreeze
		{
			get { return CurrentOrder != null && CurrentOrder.Frozen && !IsSentSelected; }
		}

		public void Unfreeze()
		{
			if (!CanUnfreeze)
				return;

			Run(new UnfreezeCommand(CurrentOrder.Id));
		}

		public bool CanReorder
		{
			get { return CurrentOrder != null && IsCurrentSelected && Orders.Count > 1; }
		}

		public void Reorder()
		{
			if (!CanReorder)
				return;

			Run(new ReorderCommand(CurrentOrder.Id));
		}

		public void EnterOrder()
		{
			if (CurrentOrder == null)
				return;

			Shell.Navigate(new OrderDetailsViewModel(CurrentOrder));
		}

		public bool CanPrint
		{
			get { return true; }
		}

		public PrintResult Print()
		{
			IEnumerable<FlowDocument> docs;
			if (!IsSentSelected) {
				docs = Orders.Select(o => new OrderDocument(o).Build());
			}
			else {
				docs = SentOrders.Select(o => new OrderDocument(o).Build());
			}
			return new PrintResult(docs, DisplayName);
		}

		public void Run(DbCommand command)
		{
			Session.Flush();
			using(var session = Session.SessionFactory.OpenSession())
			using(var transaction = session.BeginTransaction()) {
				command.Session = session;
				command.Execute();
				transaction.Commit();
			}
			Update();
		}
	}
}
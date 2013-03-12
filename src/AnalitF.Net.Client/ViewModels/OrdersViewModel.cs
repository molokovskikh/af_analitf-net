using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using Common.Tools.Calendar;
using NHibernate.Linq;
using ReactiveUI;
using log4net;

namespace AnalitF.Net.Client.ViewModels
{
	public class OrdersViewModel : BaseOrderViewModel, IPrintable
	{
		private Order currentOrder;
		private IList<Order> orders;
		private IList<SentOrder> sentOrders;
		private SentOrder currentSentOrder;
		private bool forceCurrentUpdate;

		public OrdersViewModel()
		{
			DisplayName = "Заказы";

			IsNotifying = false;
			Begin = DateTime.Today.AddMonths(-3).FirstDayOfMonth();
			End = DateTime.Today;
			IsNotifying = true;

			this.ObservableForProperty(m => (object)m.CurrentOrder)
				.Merge(this.ObservableForProperty(m => (object)m.IsCurrentSelected))
				.Subscribe(_ => {
					NotifyOfPropertyChange("CanDelete");
					NotifyOfPropertyChange("CanFreeze");
					NotifyOfPropertyChange("CanUnfreeze");
					NotifyOfPropertyChange("CanReorder");
				});

			this.ObservableForProperty(m => m.IsSentSelected)
				.Subscribe(_ => {
					NotifyOfPropertyChange("RestoreVisible");
					NotifyOfPropertyChange("CanReorder");
					NotifyOfPropertyChange("EditableOrder");
				});

			this.ObservableForProperty(m => m.IsCurrentSelected)
				.Subscribe(_ => {
					NotifyOfPropertyChange("FreezeVisible");
					NotifyOfPropertyChange("UnfreezeVisible");
					NotifyOfPropertyChange("EditableOrder");
				});

			this.ObservableForProperty(m => m.CurrentSentOrder)
				.Subscribe(_ => {
					NotifyOfPropertyChange("CanDelete");
					NotifyOfPropertyChange("CanRestore");
					NotifyOfPropertyChange("CanReorder");
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
				SentOrders = new ObservableCollection<SentOrder>(StatelessSession.Query<SentOrder>()
					.Where(o => o.SentOn >= Begin && o.SentOn < End.AddDays(1))
					.Fetch(o => o.Price)
					.OrderBy(o => o.SentOn)
					.Take(1000)
					.ToList());
			}

			if (IsCurrentSelected || forceCurrentUpdate) {
				forceCurrentUpdate = false;
				Session.Flush();
				Session.Clear();
				Address = Session.Load<Address>(Address.Id);
				Orders = new BindingList<Order>(Address.Orders.OrderBy(o => o.CreatedOn).ToList());
				var observable = Observable.FromEventPattern<ListChangedEventArgs>(Orders, "ListChanged")
					.Throttle(Consts.RefreshOrderStatTimeout, UiScheduler)
					.Select(e => new Stat(Address));
				Bus.RegisterMessageSource(observable);
			}
		}

		public IOrder EditableOrder
		{
			get
			{
				if (IsCurrentSelected)
					return CurrentOrder;
				if (IsSentSelected)
					return CurrentSentOrder;
				return null;
			}
		}

		[Export]
		public IList<Order> Orders
		{
			get { return orders; }
			set
			{
				orders = value;
				NotifyOfPropertyChange("Orders");
			}
		}

		public Order CurrentOrder
		{
			get { return currentOrder; }
			set
			{
				currentOrder = value;
				NotifyOfPropertyChange("CurrentOrder");
				NotifyOfPropertyChange("EditableOrder");
			}
		}

		[Export]
		public IList<SentOrder> SentOrders
		{
			get { return sentOrders; }
			set
			{
				sentOrders = value;
				NotifyOfPropertyChange("SentOrders");
			}
		}

		public SentOrder CurrentSentOrder
		{
			get { return currentSentOrder; }
			set
			{
				if (currentSentOrder != null)
					currentSentOrder.PropertyChanged -= WatchForUpdate;

				currentSentOrder = value;

				if (currentSentOrder != null)
					currentSentOrder.PropertyChanged += WatchForUpdate;

				NotifyOfPropertyChange("CurrentSentOrder");
				NotifyOfPropertyChange("EditableOrder");
			}
		}

		private void WatchForUpdate(object sender, PropertyChangedEventArgs e)
		{
			StatelessSession.Update(sender);
		}

		public bool CanDelete
		{
			get { return (CurrentOrder != null && IsCurrentSelected)
				|| (CurrentSentOrder != null && IsSentSelected); }
		}

		public void Delete()
		{
			if (!CanDelete)
				return;

			if (!Confirm("Удалить выбранные заявки?"))
				return;

			if (IsCurrentSelected)
				DeleteOrder(CurrentOrder);
			else {
				StatelessSession.Delete(CurrentSentOrder);
				SentOrders.Remove(CurrentSentOrder);
			}
		}

		private void DeleteOrder(Order order)
		{
			Session.Delete(order);
			if (order.Address != null) {
				order.Address.Orders.Remove(order);
			}
			Orders.Remove(order);
		}

		public bool FreezeVisible
		{
			get { return IsCurrentSelected; }
		}

		public bool CanFreeze
		{
			get { return CurrentOrder != null && !CurrentOrder.Frozen && !IsSentSelected; }
		}

		public void Freeze()
		{
			if (!CanFreeze)
				return;

			if (!Confirm("\"Заморозить\" выбранные заявки?"))
				return;

			CurrentOrder.Frozen = true;
			CurrentOrder.Send = false;
		}

		public bool UnfreezeVisible
		{
			get { return IsCurrentSelected; }
		}

		public bool CanUnfreeze
		{
			get { return CurrentOrder != null && CurrentOrder.Frozen && !IsSentSelected; }
		}

		public IResult Unfreeze()
		{
			if (!CanUnfreeze)
				return null;

			if (!Confirm("Внимание! \"Размороженные\" заявки будут объединены с текущими заявками.\r\n\r\n\"Разморозить\" выбранные заявки?"))
				return null;

			return Run(new UnfreezeCommand<Order>(CurrentOrder.Id));
		}

		public bool CanReorder
		{
			get
			{
				return ((CurrentOrder != null && IsCurrentSelected && Orders.Count > 1)
						|| (CurrentSentOrder != null && IsSentSelected && Orders.Count > 0));
			}
		}

		public IResult Reorder()
		{
			if (!CanReorder)
				return null;

			if (!Confirm("Перераспределить выбранную заявку на других поставщиков?"))
				return null;

			//'Перемещать в прайс-лист можно только по одной заявке.'
			//'Перемещать в прайс-лист можно только заявки текущего адреса заказа.'
			if (IsCurrentSelected)
				return Run(new ReorderCommand<Order>(CurrentOrder.Id));
			else
				return Run(new ReorderCommand<SentOrder>(CurrentSentOrder.Id));
		}

		public bool CanRestore
		{
			get { return CurrentSentOrder != null && IsSentSelected; }
		}

		public bool RestoreVisible
		{
			get { return IsSentSelected; }
		}

		public IResult Restore()
		{
			if (!CanRestore)
				return null;

			if (!Confirm("Вернуть выбранные заявки в работу?"))
				return null;

			return Run(new UnfreezeCommand<SentOrder>(CurrentSentOrder.Id));
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

		public IResult Run(DbCommand command)
		{
			Session.Flush();
			using(var session = Session.SessionFactory.OpenSession())
			using(var transaction = session.BeginTransaction()) {
				command.Session = session;
				command.Execute();
				forceCurrentUpdate = session.IsDirty();
				transaction.Commit();
			}
			Update();

			return ProcessCommandResult(command);
		}

		private static IResult ProcessCommandResult(DbCommand command)
		{
			var text = command.Result as string;
			if (!String.IsNullOrEmpty(text)) {
				return new DialogResult(new TextViewModel(text)) {
					ShowFixed = true
				};
			}
			return null;
		}
	}
}
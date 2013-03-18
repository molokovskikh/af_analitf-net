﻿using System;
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
using AnalitF.Net.Client.ViewModels.Parts;
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

			AddressSelector = new AddressSelector(Session, Scheduler, this);
			SelectedOrders = new List<Order>();
			SelectedSentOrders = new List<SentOrder>();

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
					NotifyOfPropertyChange("CanMove");
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
					NotifyOfPropertyChange("MoveVisible");
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

			this.ObservableForProperty(m => m.AddressSelector.All.Value)
				.Subscribe(_ => Update());

			var ordersChanged = this.ObservableForProperty(m => m.Orders);
			var update = ordersChanged
				.SelectMany(e => Observable.FromEventPattern<ListChangedEventArgs>(e.Value, "ListChanged"));

			var observable = ordersChanged.Cast<object>()
				.Merge(update)
				.Throttle(Consts.RefreshOrderStatTimeout, UiScheduler)
				.Select(e => new Stat(Address));

			Bus.RegisterMessageSource(observable);
		}

		public AddressSelector AddressSelector { get; set; }

		public List<Address> AddressesToMove { get; set; }

		public Address AddressToMove { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			AddressesToMove = Session.Query<Address>()
				.Where(a => a != Address)
				.OrderBy(a => a.Name)
				.ToList();
		}

		protected override void OnActivate()
		{
			base.OnActivate();

			Update();
		}

		public override void Update()
		{
			if (IsSentSelected) {
				var filterAddresses = AddressFilter();
				SentOrders = new ObservableCollection<SentOrder>(StatelessSession.Query<SentOrder>()
					.Where(o => o.SentOn >= Begin && o.SentOn < End.AddDays(1)
						&& filterAddresses.Contains(o.Address))
					.Fetch(o => o.Price)
					.Fetch(o => o.Address)
					.OrderBy(o => o.SentOn)
					.Take(1000)
					.ToList());
			}

			if (IsCurrentSelected || forceCurrentUpdate) {
				if (forceCurrentUpdate)
					RebuildSessionIfNeeded();

				//этот вызов должен быть после RebuildSessionIfNeeded
				//тк он перазагрузить объекты
				var filterAddresses = AddressFilter();
				var orders = filterAddresses.SelectMany(a => a.Orders)
					.OrderBy(o => o.CreatedOn)
					.ToList();
				Orders = new BindingList<Order>(orders);
			}
		}

		private Address[] AddressFilter()
		{
			var filterAddresses = new Address[0];
			if (AddressSelector.All.Value) {
				filterAddresses = AddressSelector.Addresses
					.Where(a => a.IsSelected)
					.Select(a => a.Item)
					.ToArray();
			}
			else if (Address != null) {
				filterAddresses = new[] { Address };
			}
			return filterAddresses;
		}

		private void RebuildSessionIfNeeded()
		{
			forceCurrentUpdate = false;
			Session.Flush();
			Session.Clear();
			//после того как мы очистили сессию нам нужно перезагрузить все объекты которые
			//были связаны с закрытой сессией иначе где нибуть позже мы попробуем обратиться
			//к объекты закрытой сессиии и получим ошибку

			//загружаем все в память что не делать лишних запросов
			var addresses = Session.Query<Address>().OrderBy(a => a.Name).ToList();

			if (Address != null)
				Address = Session.Load<Address>(Address.Id);

			foreach (var item in AddressSelector.Addresses)
				item.Item = Session.Load<Address>(item.Item.Id);

			if (AddressToMove != null)
				AddressToMove = Session.Load<Address>(AddressToMove.Id);

			AddressesToMove = addresses.Where(a => a != Address)
				.OrderBy(a => a.Name)
				.ToList();
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

		public List<Order> SelectedOrders { get; set; }

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

		public List<SentOrder> SelectedSentOrders { get; set; }

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

			if (IsCurrentSelected) {
				foreach (var selected in SelectedOrders.ToArray()) {
					DeleteOrder(selected);
				}
			}
			else {
				foreach (var selected in SelectedSentOrders.ToArray()) {
					StatelessSession.Delete(selected);
					SentOrders.Remove(selected);
				}
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

			foreach (var selectedOrder in SelectedOrders) {
				selectedOrder.Frozen = true;
				selectedOrder.Send = false;
			}
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

			var ids = SelectedOrders.Where(o => o.Frozen).Select(o => o.Id).ToArray();
			return Run(new UnfreezeCommand<Order>(ids));
		}

		public bool CanReorder
		{
			get
			{
				return Address != null && ((CurrentOrder != null && IsCurrentSelected && Orders.Count > 1)
						|| (CurrentSentOrder != null && IsSentSelected && Orders.Count > 0));
			}
		}

		public IResult Reorder()
		{
			if (!CanReorder)
				return null;

			if (!Confirm("Перераспределить выбранную заявку на других поставщиков?"))
				return null;

			if (IsCurrentSelected) {

				if (SelectedOrders.Count > 1) {
					Manager.Error("Перемещать в прайс-лист можно только по одной заявке.");
					return null;
				}

				if (CurrentOrder.Address != null && CurrentOrder.Address.Id != Address.Id) {
					Manager.Error("Перемещать в прайс-лист можно только заявки текущего адреса заказа.");
					return null;
				}

				return Run(new ReorderCommand<Order>(CurrentOrder.Id));
			}
			else {

				if (SelectedSentOrders.Count > 1) {
					Manager.Error("Перемещать в прайс-лист можно только по одной заявке.");
					return null;
				}

				if (CurrentSentOrder.Address != null && CurrentSentOrder.Address.Id != Address.Id) {
					Manager.Error("Перемещать в прайс-лист можно только заявки текущего адреса заказа.");
					return null;
				}

				return Run(new ReorderCommand<SentOrder>(CurrentSentOrder.Id));
			}
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

			var ids = SelectedSentOrders.Select(o => o.Id).ToArray();
			return Run(new UnfreezeCommand<SentOrder>(ids));
		}

		public bool CanMove
		{
			get { return IsCurrentSelected && CurrentOrder != null && AddressesToMove.Count > 0; }
		}

		public bool MoveVisible
		{
			get { return IsCurrentSelected; }
		}

		public IResult Move()
		{
			if (!CanMove)
				return null;

			if (!Confirm("Внимание! Перемещаемые заявки будут объединены с текущими заявками.\r\n\r\nПеренести выбранные заявки?"))
				return null;

			var ids = SelectedOrders.Where(o => o.Address == Address).Select(o => o.Id).ToArray();
			return Run(new UnfreezeCommand<Order>(ids, AddressToMove));
		}

		public void EnterOrder()
		{
			if (CurrentOrder == null)
				return;

			Shell.Navigate(new OrderDetailsViewModel(CurrentOrder));
		}

		public void EnterSentOrder()
		{
			if (CurrentSentOrder == null)
				return;

			Shell.Navigate(new OrderDetailsViewModel(CurrentSentOrder));
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
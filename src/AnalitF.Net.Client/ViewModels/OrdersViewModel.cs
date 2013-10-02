using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
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
using NPOI.SS.Formula.Functions;
using ReactiveUI;
using log4net;
using Address = AnalitF.Net.Client.Models.Address;

namespace AnalitF.Net.Client.ViewModels
{
	[DataContract]
	public class OrdersViewModel : BaseOrderViewModel, IPrintable
	{
		private Order currentOrder;
		private IList<Order> orders;
		private IList<SentOrder> sentOrders;
		private SentOrder currentSentOrder;

		public OrdersViewModel()
		{
			DisplayName = "Заказы";

			AddressSelector = new AddressSelector(Session, Scheduler, this);
			SelectedOrders = new List<Order>();
			SelectedSentOrders = new List<SentOrder>();

			Begin.Mute(DateTime.Today.AddMonths(-3).FirstDayOfMonth());
			End.Mute(DateTime.Today);

			OnCloseDisposable.Add(this.ObservableForProperty(m => (object)m.CurrentOrder)
				.Merge(this.ObservableForProperty(m => (object)m.IsCurrentSelected.Value))
				.Subscribe(_ => {
					NotifyOfPropertyChange("CanDelete");
					NotifyOfPropertyChange("CanFreeze");
					NotifyOfPropertyChange("CanUnfreeze");
					NotifyOfPropertyChange("CanReorder");
					NotifyOfPropertyChange("CanMove");
				}));

			OnCloseDisposable.Add(this.ObservableForProperty(m => m.IsSentSelected)
				.Subscribe(_ => {
					NotifyOfPropertyChange("RestoreVisible");
					NotifyOfPropertyChange("CanReorder");
					NotifyOfPropertyChange("EditableOrder");
				}));

			OnCloseDisposable.Add(IsCurrentSelected
				.Changed()
				.Subscribe(_ => {
					NotifyOfPropertyChange("FreezeVisible");
					NotifyOfPropertyChange("UnfreezeVisible");
					NotifyOfPropertyChange("MoveVisible");
					NotifyOfPropertyChange("EditableOrder");
				}));

			OnCloseDisposable.Add(this.ObservableForProperty(m => m.CurrentSentOrder)
				.Subscribe(_ => {
					NotifyOfPropertyChange("CanDelete");
					NotifyOfPropertyChange("CanRestore");
					NotifyOfPropertyChange("CanReorder");
				}));

			OnCloseDisposable.Add(this.ObservableForProperty(m => m.CurrentOrder.Frozen)
				.Subscribe(_ => {
					NotifyOfPropertyChange("CanFreeze");
					NotifyOfPropertyChange("CanUnfreeze");
				}));

			this.ObservableForProperty(m => m.AddressSelector.All.Value)
				.Subscribe(_ => Update());

			var ordersChanged = this.ObservableForProperty(m => m.Orders);
			var update = ordersChanged
				.SelectMany(e => e.Value.Changed());

			var observable = ordersChanged.Cast<object>()
				.Merge(update)
				.Throttle(Consts.RefreshOrderStatTimeout, UiScheduler)
				.Select(e => new Stat(Address));

			OnCloseDisposable.Add(Bus.RegisterMessageSource(observable));
		}

		public AddressSelector AddressSelector { get; set; }

		public List<Address> AddressesToMove { get; set; }

		public Address AddressToMove { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			AddressSelector.Init();
			var addressId = Address != null ? Address.Id : 0;
			AddressesToMove = Session.Query<Address>()
				.Where(a => a.Id != addressId)
				.OrderBy(a => a.Name)
				.ToList();
		}

		public override void Update()
		{
			if (IsSentSelected) {
				var filterAddresses = AddressFilter().Select(a => a.Id).ToArray();
				var begin = Begin.Value;
				var end = End.Value.AddDays(1);
				SentOrders = new ObservableCollection<SentOrder>(StatelessSession.Query<SentOrder>()
					.Where(o => o.SentOn >= begin && o.SentOn < end
						&& filterAddresses.Contains(o.Address.Id))
					.OrderBy(o => o.SentOn)
					.Fetch(o => o.Price)
					.Fetch(o => o.Address)
					.Take(1000)
					.ToList());
			}

			//обновить данные нужно в нескольких ситуациях
			//изменился фильтр
			//изменились данные в базе
			//если изменился фильтры данные загружать не нужно
			//если изменились данные то нужно загрузить данные повторно
			if (IsCurrentSelected || updateOnActivate) {
				if (updateOnActivate)
					RebuildSessionIfNeeded();

				//этот вызов должен быть после RebuildSessionIfNeeded
				//тк он перезагрузить объекты
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
			Session.Flush();
			Session.Clear();
			//после того как мы очистили сессию нам нужно перезагрузить все объекты которые
			//были связаны с закрытой сессией иначе где нибудь позже мы попробуем обратиться
			//к объекты закрытой сессии и получим ошибку

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
				docs = SentOrders.Select(BuildPrintOrderDocument);
			}
			return new PrintResult(DisplayName, docs);
		}

		private FlowDocument BuildPrintOrderDocument(SentOrder order)
		{
			var id = order.Id;
			var result = StatelessSession.Query<SentOrder>()
				.Where(o => o.Id == id)
				.Fetch(o => o.Price)
				.Fetch(o => o.Address)
				.Fetch(o => o.Lines)
				.First();
			return new OrderDocument(result).Build();
		}

		public IResult Run(DbCommand command)
		{
			Session.Flush();
			using(var session = Session.SessionFactory.OpenSession())
			using(var transaction = session.BeginTransaction()) {
				command.Session = session;
				command.Execute();
				updateOnActivate = session.IsDirty();
				transaction.Commit();
			}
			Update();

			return ProcessCommandResult(command);
		}

		private static IResult ProcessCommandResult(DbCommand command)
		{
			var text = command.Result as string;
			if (!String.IsNullOrEmpty(text))
				return new DialogResult(new TextViewModel(text), @fixed: true);

			return null;
		}
	}
}
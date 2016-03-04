using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
using Common.Tools;
using Common.Tools.Calendar;
using NHibernate.Linq;
using ReactiveUI;
using Address = AnalitF.Net.Client.Models.Address;

namespace AnalitF.Net.Client.ViewModels.Orders
{
	[DataContract]
	public class OrdersViewModel : BaseOrderViewModel, IPrintable
	{
		private Order currentOrder;
		private ReactiveCollection<Order> orders;
		private IList<SentOrder> sentOrders;
		private SentOrder currentSentOrder;

		public OrdersViewModel()
		{
			DisplayName = "Заказы";

			AddressSelector = new AddressSelector(this);
			SelectedOrders = new List<Order>();
			SelectedSentOrders = new List<SentOrder>();

			Begin.Mute(DateTime.Today.AddMonths(-3).FirstDayOfMonth());
			End.Mute(DateTime.Today);

			OnCloseDisposable.Add(this.ObservableForProperty(m => (object)m.CurrentOrder)
				.Merge(this.ObservableForProperty(m => (object)m.IsCurrentSelected.Value))
				.Subscribe(_ => {
					NotifyOfPropertyChange(nameof(CanDelete));
					NotifyOfPropertyChange(nameof(CanFreeze));
					NotifyOfPropertyChange(nameof(CanUnfreeze));
					NotifyOfPropertyChange(nameof(CanReorder));
					NotifyOfPropertyChange(nameof(CanMove));
				}));

			OnCloseDisposable.Add(IsSentSelected
				.Subscribe(_ => {
					NotifyOfPropertyChange(nameof(RestoreOrderVisible));
					NotifyOfPropertyChange(nameof(CanReorder));
					NotifyOfPropertyChange(nameof(EditableOrder));
				}));

			OnCloseDisposable.Add(IsCurrentSelected
				.Subscribe(_ => {
					NotifyOfPropertyChange(nameof(FreezeVisible));
					NotifyOfPropertyChange(nameof(UnfreezeVisible));
					NotifyOfPropertyChange(nameof(MoveVisible));
					NotifyOfPropertyChange(nameof(EditableOrder));
				}));

			OnCloseDisposable.Add(this.ObservableForProperty(m => m.CurrentSentOrder)
				.Subscribe(_ => {
					NotifyOfPropertyChange(nameof(CanDelete));
					NotifyOfPropertyChange(nameof(CanRestoreOrder));
					NotifyOfPropertyChange(nameof(CanReorder));
				}));

			OnCloseDisposable.Add(this.ObservableForProperty(m => m.CurrentOrder.Frozen)
				.Subscribe(_ => {
					NotifyOfPropertyChange(nameof(CanFreeze));
					NotifyOfPropertyChange(nameof(CanUnfreeze));
				}));

			var ordersChanged = this.ObservableForProperty(m => m.Orders);
			var update = ordersChanged
				.SelectMany(e => e.Value.ItemChanged.Cast<Object>().Merge(e.Value.Changed));

			var observable = ordersChanged.Cast<object>()
				.Merge(update)
				.Throttle(Consts.RefreshOrderStatTimeout, UiScheduler)
				.Select(e => new Stat(Address));

			OnCloseDisposable.Add(Bus.RegisterMessageSource(observable));
			IsCurrentSelected
				.Select(v => v ? "Orders" : "SentOrders")
				.Subscribe(ExcelExporter.ActiveProperty);
		}

		public AddressSelector AddressSelector { get; set; }

		public List<Address> AddressesToMove { get; set; }

		public Address AddressToMove { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			AddressSelector.Init();
			AddressSelector.FilterChanged.Subscribe(_ => Update(), CloseCancellation.Token);

			var addressId = (Address?.Id).GetValueOrDefault();
			if (Session != null) {
				AddressesToMove = Session.Query<Address>()
					.Where(a => a.Id != addressId)
					.OrderBy(a => a.Name)
					.ToList();
			}
		}

		protected override void OnDeactivate(bool close)
		{
			AddressSelector.OnDeactivate();
			base.OnDeactivate(close);
		}

		public override void Update()
		{
			if (IsSentSelected) {
				var filterAddresses = AddressSelector.GetActiveFilter().Select(a => a.Id).ToArray();
				var begin = Begin.Value;
				var end = End.Value.AddDays(1);
				SentOrders = StatelessSession.Query<SentOrder>()
					.Where(o => o.SentOn >= begin && o.SentOn < end
						&& filterAddresses.Contains(o.Address.Id))
					.OrderByDescending(o => o.SentOn)
					.Fetch(o => o.Price)
					.Fetch(o => o.Address)
					.Take(1000)
					.ToObservableCollection();
				SentOrders.Each(o => o.CalculateStyle(Address));
			}

			//обновить данные нужно в нескольких ситуациях
			//изменился фильтр
			//изменились данные в базе
			//если изменился фильтры данные загружать не нужно
			//если изменились данные то нужно загрузить данные повторно
			if (IsCurrentSelected || UpdateOnActivate) {
				if (UpdateOnActivate)
					RebuildSessionIfNeeded();

				//этот вызов должен быть после RebuildSessionIfNeeded
				//тк он перезагрузить объекты
				var orders = AddressSelector.GetActiveFilter()
					.SelectMany(a => a.Orders)
					.OrderBy(o => o.PriceName)
					.ToList();
				orders.Each(o => o.CalculateStyle(Address));
				if (CurrentOrder != null)
					CurrentOrder = orders.FirstOrDefault(x => x.Id == CurrentOrder.Id);
				Orders = new ReactiveCollection<Order>(orders) {
					ChangeTrackingEnabled = true
				};

				Price.LoadOrderStat(orders.Select(o => o.Price), Address, StatelessSession);
			}
		}

		private void RebuildSessionIfNeeded()
		{
			if (Session == null)
				return;
			Session.Flush();
			Session.Clear();
			//после того как мы очистили сессию нам нужно перезагрузить все объекты которые
			//были связаны с закрытой сессией иначе где нибудь позже мы попробуем обратиться
			//к объекты закрытой сессии и получим ошибку

			//загружаем все в память что не делать лишних запросов
			var addresses = Session.Query<Address>().OrderBy(a => a.Name).ToList();

			if (Address != null)
				Address = Session.Load<Address>(Address.Id);
			User = Session.Get<User>(User.Id) ?? User;

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

		//используется ReactiveCollection тк нужно отслеживать состояние флага отправить
		//для установки состояния кнопки отправить
		//BindingList - не пригоден тк он запрещает сортировку
		[Export]
		public ReactiveCollection<Order> Orders
		{
			get { return orders; }
			set
			{
				orders = value;
				NotifyOfPropertyChange(nameof(Orders));
			}
		}

		public Order CurrentOrder
		{
			get { return currentOrder; }
			set
			{
				currentOrder = value;
				NotifyOfPropertyChange(nameof(CurrentOrder));
				NotifyOfPropertyChange(nameof(EditableOrder));
			}
		}

		[Export]
		public IList<SentOrder> SentOrders
		{
			get { return sentOrders; }
			set
			{
				sentOrders = value;
				NotifyOfPropertyChange(nameof(SentOrders));
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

				NotifyOfPropertyChange(nameof(CurrentSentOrder));
				NotifyOfPropertyChange(nameof(EditableOrder));
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
					Log.Info($"Удаление отправленного заказа {selected.DisplayId} дата отправки: {selected.SentOn}" +
						$" прайс-лист: {selected.SafePrice?.Name}" +
						$" позиций: {selected.LinesCount}");
					StatelessSession.Delete(selected);
					SentOrders.Remove(selected);
				}
			}
		}

		private void DeleteOrder(Order order)
		{
			Log.Info($"Удаление текущего заказа {order.DisplayId} дата создания: {order.CreatedOn}" +
				$" прайс-лист: {order.SafePrice?.Name}" +
				$" позиций: {order.LinesCount}");
			Session.Delete(order);
			if (order.Address != null) {
				order.Address.Orders.Remove(order);
			}
			Orders.Remove(order);
		}

		public bool FreezeVisible => IsCurrentSelected;

		public bool CanFreeze => CurrentOrder?.Frozen == false && !IsSentSelected;

		public void Freeze()
		{
			if (!CanFreeze)
				return;

			if (!Confirm("\"Заморозить\" выбранные заявки?"))
				return;

			foreach (var selectedOrder in SelectedOrders)
				selectedOrder.Frozen = true;
			Log.Info($"Заморожены заявки {SelectedOrders.Implode(x => x.Id)}");
		}

		public bool UnfreezeVisible => IsCurrentSelected;

		public bool CanUnfreeze => CurrentOrder?.Frozen == true && !IsSentSelected;

		public IEnumerable<IResult> Unfreeze()
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

		public IEnumerable<IResult> Reorder()
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

		public bool CanRestoreOrder
		{
			get { return CurrentSentOrder != null && IsSentSelected; }
		}

		public bool RestoreOrderVisible => IsSentSelected;

		public IEnumerable<IResult> RestoreOrder()
		{
			if (!CanRestoreOrder)
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

		public bool MoveVisible => IsCurrentSelected;

		public IEnumerable<IResult> Move()
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
			get
			{
				if (IsCurrentSelected)
					return User.CanPrint<OrderDocument, Order>();
				return User.CanPrint<OrderDocument, SentOrder>();
			}
		}

		public PrintResult Print()
		{
			IEnumerable<BaseDocument> docs;
			if (!IsSentSelected) {
				docs = Orders.Select(o => new OrderDocument(o));
			}
			else {
				docs = SentOrders.Select(BuildPrintOrderDocument);
			}
			return new PrintResult(DisplayName, docs);
		}

		private BaseDocument BuildPrintOrderDocument(SentOrder order)
		{
			var id = order.Id;
			var result = StatelessSession.Query<SentOrder>()
				.Where(o => o.Id == id)
				.Fetch(o => o.Price)
				.Fetch(o => o.Address)
				.Fetch(o => o.Lines)
				.ToList()
				.First();
			return new OrderDocument(result);
		}

		public IEnumerable<IResult> Run(DbCommand command)
		{
			Session.Flush();
			var task = new Task(() => {
				using(var session = Session.SessionFactory.OpenSession())
				using(var stateless = Session.SessionFactory.OpenStatelessSession())
				using(var transaction = session.BeginTransaction()) {
					command.Session = session;
					command.StatelessSession = stateless;
					command.Config = Shell.Config;
					command.Execute();
					UpdateOnActivate = session.IsDirty();
					transaction.Commit();
				}
			});

			yield return new Models.Results.TaskResult(task);
			Update();
			var text = command.Result as string;
			if (!String.IsNullOrEmpty(text))
				yield return new DialogResult(new TextViewModel(text));
		}
	}
}
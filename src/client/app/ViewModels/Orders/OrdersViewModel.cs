using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using AnalitF.Net.Client.Controls;
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
using NHibernate.Proxy;
using ReactiveUI;
using Address = AnalitF.Net.Client.Models.Address;
using Common.NHibernate;

namespace AnalitF.Net.Client.ViewModels.Orders
{
	[DataContract]
	public class OrdersViewModel : BaseOrderViewModel, IPrintable
	{
		private Order currentOrder;
		private ReactiveCollection<Order> orders;
		private IList<SentOrder> sentOrders;
		private SentOrder currentSentOrder;
		private ReactiveCollection<DeletedOrder> deletedOrders;
		private DeletedOrder currentDeletedOrder;

		public OrdersViewModel()
		{
			DisplayName = "Заказы";

			InitFields();
			AddressSelector = new AddressSelector(this);
			SelectedOrders = new List<Order>();
			SelectedSentOrders = new List<SentOrder>();
			SelectedDeletedOrders = new List<DeletedOrder>();
			deletedOrders = new ReactiveCollection<DeletedOrder>();

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

			OnCloseDisposable.Add(IsDeletedSelected
				.Subscribe(_ => {
					NotifyOfPropertyChange(nameof(UnDeleteVisible));
					NotifyOfPropertyChange(nameof(ReorderVisible));
				}));

			OnCloseDisposable.Add(this.ObservableForProperty(m => m.CurrentDeletedOrder)
				.Subscribe(_ => {
					NotifyOfPropertyChange(nameof(CanDelete));
					NotifyOfPropertyChange(nameof(CanUnDelete));
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

		public NotifyValue<List<Selectable<Price>>> Prices { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			AddressSelector.Init();
			AddressSelector.FilterChanged.Cast<object>()
				.Merge(Prices.SelectMany(x => x?.Select(p => p.Changed()).Merge().Throttle(Consts.FilterUpdateTimeout, UiScheduler)
					?? Observable.Empty<EventPattern<PropertyChangedEventArgs>>()))
				.Merge(Prices.Where(x => x != null))
				.Subscribe(_ => Update(), CloseCancellation.Token);
			AddressesToMove = Addresses.Where(x => x != Address).ToList();

			RxQuery(s => s.Query<Price>().OrderBy(x => x.Name).Select(x => new Selectable<Price>(x)).ToList())
				.Subscribe(Prices, CloseCancellation.Token);
		}

		protected override void OnDeactivate(bool close)
		{
			AddressSelector.OnDeactivate();
			base.OnDeactivate(close);
		}

		public override void Update()
		{
			//до загрузки прайс-листов избегаем выбирать данные что бы не делать лишних запросов
			if (Prices.Value == null)
				return;
			var priceIds = Prices.Value.Where(x => x.IsSelected).Select(x => x.Item.Id).ToArray();

			if (IsSentSelected) {
				var filterAddresses = AddressSelector.GetActiveFilter().Select(a => a.Id).ToArray();
				var begin = Begin.Value;
				var end = End.Value.AddDays(1);
				Env.RxQuery(s => {
					var query = s.Query<SentOrder>()
						.Fetch(o => o.Price)
						.Fetch(o => o.Address)
						.Where(o => o.SentOn >= begin && o.SentOn < end
							&& filterAddresses.Contains(o.Address.Id));
					query = Util.Filter(query, o => o.Price.Id, Prices.Value);
					return query
						.OrderByDescending(o => o.SentOn)
						.Take(1000)
						.ToObservableCollection();
				}).CatchSubscribe(x => {
					for(var i = 0; i < x.Count; i++)
						x[i].CalculateStyle(Address);
					SentOrders = x;
				});
			}

			if (IsDeletedSelected) {
				Env.RxQuery(s => {
					var result = s.Query<DeletedOrder>()
					.Fetch(o => o.Price)
					.Fetch(o => o.Address)
					.OrderByDescending(o => o.DeletedOn)
					.ToList();
					if (CurrentDeletedOrder != null)
						CurrentDeletedOrder = result.FirstOrDefault(x => x.Id == CurrentDeletedOrder.Id);
					return new ReactiveCollection<DeletedOrder>(result)
					{
						ChangeTrackingEnabled = true
					};
				}).CatchSubscribe(x => { DeletedOrders = x; });
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
				var orders = new List<Order>();
				if (IsSelectedAllAddress() && AddressSelector.All) {
					orders = Session.Query<Order>().ToList()
						.Where(x => priceIds.Contains(x.Price.Id) || IsSelectedAllPrices())
						.OrderBy(o => o.PriceName)
						.ToList();
				}
				else {
				orders = AddressSelector.GetActiveFilter()
					.SelectMany(a => a.Orders)
					.Where(x => priceIds.Contains(x.SafePrice.Id) || IsSelectedAllPrices())
					.OrderBy(o => o.PriceName)
					.ToList();
					orders.Each(o => o.CalculateStyle(Address));
				}
				if (CurrentOrder != null)
					CurrentOrder = orders.FirstOrDefault(x => x.Id == CurrentOrder.Id);
				Orders = new ReactiveCollection<Order>(orders) {
					ChangeTrackingEnabled = true
				};
				Price.LoadOrderStat(Env, orders.Select(o => o.Price), Address).LogResult();
			}
		}

		private bool IsSelectedAllPrices()
		{
			if(Prices.Value.Where(x => x.IsSelected).ToArray().Length == Prices.Value.Count)
				return true;
			return false;
		}
		private bool IsSelectedAllAddress()
		{
			if(AddressSelector.Addresses.Where(x => x.IsSelected).ToArray().Length == AddressSelector.Addresses.Count
				&& AddressSelector.Addresses.Count != 0)
				return true;
			return false;
		}

		private void RebuildSessionIfNeeded()
		{
			if (Session == null)
				return;

			Session.Flush();
			RecreateSession();
			//после того как мы очистили сессию нам нужно перезагрузить все объекты которые
			//были связаны с закрытой сессией иначе где нибудь позже мы попробуем обратиться
			//к объекты закрытой сессии и получим ошибку
			//загружаем все в память что не делать лишних запросов
			foreach (var item in AddressSelector.Addresses)
				item.Item = Session.Load<Address>(item.Item.Id);

			if (AddressToMove != null)
				AddressToMove = Session.Load<Address>(AddressToMove.Id);

			AddressesToMove = Addresses.Where(a => a != Address)
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

		// список текущих заказов
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

		public ReactiveCollection<DeletedOrder> DeletedOrders
		{
			get { return deletedOrders; }
			set
			{
				deletedOrders = value;
				NotifyOfPropertyChange(nameof(DeletedOrders));
			}
		}

		public List<DeletedOrder> SelectedDeletedOrders { get; set; }

		public DeletedOrder CurrentDeletedOrder
		{
			get { return currentDeletedOrder; }
			set
			{
				if (currentDeletedOrder != null)
					currentDeletedOrder.PropertyChanged -= WatchForUpdate;

				currentDeletedOrder = value;

				if (currentDeletedOrder != null)
					currentDeletedOrder.PropertyChanged += WatchForUpdate;

				NotifyOfPropertyChange(nameof(CurrentDeletedOrder));
			}
		}

		public bool CanDelete
		{
			get { return (CurrentOrder != null && IsCurrentSelected)
				|| (CurrentSentOrder != null && IsSentSelected)
				|| (CurrentDeletedOrder != null && IsDeletedSelected); }
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
			else if (IsSentSelected)
			{
				foreach (var selected in SelectedSentOrders.ToArray()) {
					Log.Info($"Удаление отправленного заказа {selected.DisplayId} дата отправки: {selected.SentOn}" +
						$" прайс-лист: {selected.SafePrice?.Name}" +
						$" позиций: {selected.LinesCount}");
					//в замыкании нельзя использовать переменную итератора
					var order = selected;
					Env.Query(s => s.Delete(order)).LogResult();
					SentOrders.Remove(selected);
				}
			}
			else if (IsDeletedSelected)
			{
				foreach (var selected in SelectedDeletedOrders.ToArray())
				{
					Log.Info($"Удаление текущего заказа {selected.DisplayId} из корзины дата создания: {selected.CreatedOn}" +
						$" прайс-лист: {selected.SafePrice?.Name}" +
						$" позиций: {selected.LinesCount}");
					var order = selected;
					Env.Query(s => s.Delete(order)).LogResult();
					DeletedOrders.Remove(selected);
				}
			}
		}

		private void DeleteOrder(Order order)
		{
			Log.Info($"Перемещение в корзину текущего заказа {order.DisplayId} дата создания: {order.CreatedOn}" +
				$" прайс-лист: {order.SafePrice?.Name}" +
				$" позиций: {order.LinesCount}");

			var deletedOrder = new DeletedOrder(order);
			Session.Save(deletedOrder);
			DeletedOrders.Add(deletedOrder);

			Session.Delete(order);
			if (order.Address != null && !order.Address.IsProxy()) {
				order.Address.Orders.Remove(order);
			}
			Orders.Remove(order);
			Session.Flush();
		}

		public bool FreezeVisible => IsCurrentSelected;

		public bool CanFreeze => CurrentOrder?.Frozen == false && IsCurrentSelected;

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

		public bool CanUnfreeze => CurrentOrder?.Frozen == true && !IsSentSelected && !IsDeletedSelected;

		public bool UnDeleteVisible => IsDeletedSelected;

		public bool CanUnDelete => IsDeletedSelected && CurrentDeletedOrder != null;

		public IEnumerable<IResult> Unfreeze()
		{
			if (!CanUnfreeze)
				return null;

			if (!Confirm("Внимание! \"Размороженные\" заявки будут объединены с текущими заявками.\r\n\r\n\"Разморозить\" выбранные заявки?"))
				return null;

			var ids = SelectedOrders.Where(o => o.Frozen).Select(o => o.Id).ToArray();
			return Run(new UnfreezeCommand<Order>(ids));
		}

		public IEnumerable<IResult> UnDelete()
		{
			if (!CanUnDelete)
				return null;

			if (!Confirm("Внимание! Восстановленные из корзины заявки будут объединены с текущими заявками.\r\n\r\nВосстановить выбранные заявки?"))
				return null;

			var ids = SelectedDeletedOrders.Select(o => o.Id).ToArray();
			return Run(new UnfreezeCommand<DeletedOrder>(ids));
		}

		public bool ReorderVisible => !IsDeletedSelected;

		public bool CanReorder
		{
			get
			{
				if (IsDeletedSelected)
					return false;
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

			var frozenProducts = Orders.Where(x => x.Frozen).SelectMany(x => x.Lines).Select(x => x.ProductId).Distinct().ToList();
			Shell.Navigate(new OrderDetailsViewModel(CurrentOrder, frozenProducts));
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
				if (IsDeletedSelected)
					return false;
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
			var result = Env.Query(s => s.Query<SentOrder>()
				.Where(o => o.Id == id)
				.Fetch(o => o.Price)
				.Fetch(o => o.Address)
				.Fetch(o => o.Lines)
				.ToList()
				.First()).Result;
			return new OrderDocument(result);
		}

		public IEnumerable<IResult> Run(DbCommand command)
		{
			Session.Flush();
			var task = new Task(() => {
				using(var session = Session.SessionFactory.OpenSession())
				using(var transaction = session.BeginTransaction())
				using(var stateless = Session.SessionFactory.OpenStatelessSession(session.Connection)) {
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
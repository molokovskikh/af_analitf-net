using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Inventory;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.ViewModels.Parts;
using Common.Tools;
using NHibernate;
using ReactiveUI;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Orders
{
	public class OrderDetailsViewModel : BaseScreen, IPrintable
	{
		private uint orderId;
		private Type type;
		private Editor editor;

		public OrderDetailsViewModel(IOrder order)
		{
			InitFields();
			orderId = order.Id;
			type = NHibernateUtil.GetClass(order);
			DisplayName = "Архивный заказ";

			Lines = new NotifyValue<IList<IOrderLine>>(new List<IOrderLine>(), Filter, OnlyWarning, LinesFilter);
			MatchedWaybills = new MatchedWaybills(this,
				CurrentLine.OfType<SentOrderLine>().ToValue(),
				new NotifyValue<bool>(!IsCurrentOrder));
			if (User.CanExport(this, type.Name))
				ExcelExporter.Properties = new []{ "Lines" };
			else
				ExcelExporter.Properties = new string[0];
			ExcelExporter.ActiveProperty.Refresh();
		}

		public IList<IOrderLine> Source { get; set; }

		public bool IsCurrentOrder => type == typeof(Order);

		public NotifyValue<bool> OnlyWarning { get; set; }
		public NotifyValue<bool> OnlyWarningVisible { get; set; }

		public InlineEditWarning OrderWarning { get; set; }

		public ProductInfo ProductInfo { get; set; }

		public NotifyValue<IOrder> Order { get; set; }

		[Export]
		public NotifyValue<IList<IOrderLine>> Lines { get; set; }

		public NotifyValue<IOrderLine> CurrentLine { get; set; }

		public MatchedWaybills MatchedWaybills { get; set; }

		public bool CanPrint => User.CanPrint<OrderDocument>(type);

		public bool ShowPriceVisible => IsCurrentOrder;

		public bool CanShowPrice => Order.Value.SafePrice != null && IsCurrentOrder;

		public NotifyValue<List<SentOrderLine>> HistoryOrders { get; set; }

		public NotifyValue<LinesFilter> LinesFilter { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			ProductInfo = new ProductInfo(this);
			OrderWarning = new InlineEditWarning(UiScheduler, Manager);

			//если это отправленный заказ редактор не должен работать
			var currentOrderLine = new NotifyValue<OrderLine>();
			if (IsCurrentOrder) {
				currentOrderLine = CurrentLine.Select(v => (OrderLine)v).ToValue();
				currentOrderLine.Subscribe(v => CurrentLine.Value = v);
			}
			editor = new Editor(OrderWarning, Manager, currentOrderLine, Lines.Cast<IList>().ToValue());
			CurrentLine
				.Subscribe(_ => ProductInfo.CurrentOffer = (BaseOffer)CurrentLine.Value);

			OnlyWarningVisible = new NotifyValue<bool>(User.IsPreprocessOrders && IsCurrentOrder);
			CurrentLine.OfType<BaseOffer>()
				.Throttle(Consts.LoadOrderHistoryTimeout, Scheduler)
				.Select(x => RxQuery(s => BaseOfferViewModel.LoadOrderHistory(s, Cache, Settings.Value, x, Address)))
				.Switch()
				.ObserveOn(UiScheduler)
				.Subscribe(HistoryOrders, CloseCancellation.Token);
		}

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);
			Attach(view as UIElement, ProductInfo.Bindings);
		}

		private IList<IOrderLine> Filter()
		{
			if (OnlyWarning || LinesFilter.Value == Orders.LinesFilter.InFrozenOrders) {
				var query = Source.OfType<OrderLine>();
				if (OnlyWarning)
					query = query.Where(x => x.SendResult != LineResultStatus.OK);
				if (LinesFilter.Value == Orders.LinesFilter.InFrozenOrders)
					query = query.Where(x => x.InFrozenOrders);
				return query.OrderBy(l => l.ProducerSynonym).LinkTo(Source);
			}

			return Source;
		}

		//todo init orderline
		public override void Update()
		{
			//Update - может быть вызван повторно если
			//мы вернулись на текущую форму с другой формы где были отредактированы данные
			if (Order.Value != null)
				Session.Evict(Order.Value);
			Order.Value = (IOrder)Session.Get(type, orderId);
			//если заказ был удален
			if (Order.Value == null) {
				IsSuccessfulActivated = false;
				return;
			}
			if (Settings.Value.HighlightUnmatchedOrderLines && !IsCurrentOrder) {
				var sentLines =  (IList<SentOrderLine>)Order.Value.Lines;
				var lookup = MatchedWaybills.GetLookUp(StatelessSession, sentLines);
				sentLines.Each(l => l.Configure(User, lookup));
			}
			// Текущие заказы
			else
			{
				Order.Value.Lines.Each(l => l.Configure(User));

				// #48323 Присутствует в замороженных заказах
				var productInFrozenOrders = StatelessSession.Query<Order>()
					.Where(x => x.Frozen)
					.SelectMany(x => x.Lines)
					.Select(x => x.ProductId)
					.ToList();
				Order.Value.Lines
					.Cast<OrderLine>()
					.Where(x => productInFrozenOrders.Contains(x.ProductId))
					.Each(x => x.InFrozenOrders = true);
			}

			if (CurrentLine.Value != null)
				CurrentLine.Value = Order.Value.Lines.FirstOrDefault(x => x.Id == CurrentLine.Value.Id);

			Source = new ObservableCollection<IOrderLine>(Order.Value.Lines.OrderBy(l => l.ProductSynonym));
			Source.ObservableForProperty(c => c.Count)
				.Where(e => e.Value == 0)
				.Subscribe(_ => TryClose());

			Lines.Recalculate();
		}

		public PrintResult Print()
		{
			//порядок сортировки должен быть такой же как в таблице
			var lines = GetItemsFromView<IOrderLine>("Lines") ?? Lines.Value;
			return new PrintResult(DisplayName, new OrderDocument(Order.Value, lines));
		}

		public void EnterLine()
		{
			ProductInfo.ShowCatalog();
		}

		public void ShowPrice()
		{
			if (!CanShowPrice)
				return;

			var offerViewModel = new PriceOfferViewModel(Order.Value.Price.Id,
				false,
				CurrentLine.Value == null ? null : ((OrderLine)CurrentLine).OfferId);
			Shell.Navigate(offerViewModel);
		}

		public void Search(TextCompositionEventArgs args)
		{
			if (!CanShowPrice)
				return;

			var model = new SearchOfferViewModel();
			model.SearchBehavior.SearchText.Value = args.Text;
			Shell.Navigate(model);
		}

		public void Delete()
		{
			editor.Delete();
		}

		public void OfferUpdated()
		{
			editor.Updated();
		}

		public void OfferCommitted()
		{
			editor.Committed();
		}

		public override void TryClose()
		{
			OfferCommitted();
			base.TryClose();
		}

		public void OpenReceivingOrder()
		{
			Shell.Navigate(new ReceivingDetails(((SentOrder)Order).ReceivingOrderId.Value));
		}
	}
}
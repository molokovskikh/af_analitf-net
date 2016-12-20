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
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.ViewModels.Parts;
using Common.Tools;
using NHibernate;
using ReactiveUI;
using NHibernate.Linq;
using NPOI.HSSF.Record.Chart;
using AnalitF.Net.Client.Controls;

namespace AnalitF.Net.Client.ViewModels.Orders
{
	public class OrderDetailsViewModel : BaseScreen, IPrintable
	{
		private uint orderId;
		private Type type;
		private Editor editor;
		private List<uint> frozenProducts;

		public OrderDetailsViewModel(IOrder order, List<uint> fProducts = null)
		{
			InitFields();
			orderId = order.Id;
			type = NHibernateUtil.GetClass(order);
			if (IsCurrentOrder)
				DisplayName = "Текущий заказ";
			else
				DisplayName = "Архивный заказ";
			Lines = new NotifyValue<IList<IOrderLine>>(new List<IOrderLine>(), Filter);
			MatchedWaybills = new MatchedWaybills(this,
				CurrentLine.OfType<SentOrderLine>().ToValue(),
				new NotifyValue<bool>(!IsCurrentOrder));
			if (User.CanExport(this, type.Name))
				ExcelExporter.Properties = new []{ "Lines" };
			else
				ExcelExporter.Properties = new string[0];
			ExcelExporter.ActiveProperty.Refresh();
			frozenProducts = fProducts ?? new List<uint>();

			FilterItems = new List<Selectable<Tuple<string, string>>>();
			FilterItems.Add(new Selectable<Tuple<string, string>>(Tuple.Create("InFrozenOrders", "Позиции присутствуют в замороженных заказах")));
			FilterItems.Add(new Selectable<Tuple<string, string>>(Tuple.Create("IsMinCost", "Позиции по мин.ценам")));
			FilterItems.Add(new Selectable<Tuple<string, string>>(Tuple.Create("IsNotMinCost", "Позиции не по мин.ценам")));
			FilterItems.Add(new Selectable<Tuple<string, string>>(Tuple.Create("OnlyWarning", "Только позиции с корректировкой")));
		}

		public List<Selectable<Tuple<string, string>>> FilterItems { get; set; }

		// все строки заказа
		public IList<IOrderLine> Source { get; set; }

		public bool IsCurrentOrder => type == typeof(Order);

		public NotifyValue<bool> OnlyWarningVisible { get; set; }

		public InlineEditWarning OrderWarning { get; set; }

		public ProductInfo ProductInfo { get; set; }

		public NotifyValue<IOrder> Order { get; set; }

		// фильтрованные строки заказа на UI
		[Export]
		public NotifyValue<IList<IOrderLine>> Lines { get; set; }

		public NotifyValue<IOrderLine> CurrentLine { get; set; }

		public MatchedWaybills MatchedWaybills { get; set; }

		public bool CanPrint => User.CanPrint<OrderDocument>(type);

		public bool ShowPriceVisible => IsCurrentOrder;

		public bool CanShowPrice => Order.Value.SafePrice != null && IsCurrentOrder;

		public NotifyValue<List<SentOrderLine>> HistoryOrders { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			ProductInfo = new ProductInfo(this, CurrentLine.OfType<BaseOffer>());
			OrderWarning = new InlineEditWarning(UiScheduler, Manager);

			//если это отправленный заказ редактор не должен работать
			var currentOrderLine = new NotifyValue<OrderLine>();
			if (IsCurrentOrder) {
				currentOrderLine = CurrentLine.Select(v => (OrderLine)v).ToValue();
				currentOrderLine.Subscribe(v => CurrentLine.Value = v);
			}
			editor = new Editor(OrderWarning, Manager, currentOrderLine, Lines.Cast<IList>().ToValue());
			OnlyWarningVisible = new NotifyValue<bool>(User.IsPreprocessOrders && IsCurrentOrder);
			CurrentLine.OfType<BaseOffer>()
				.Throttle(Consts.LoadOrderHistoryTimeout, Scheduler)
				.Select(x => RxQuery(s => BaseOfferViewModel.LoadOrderHistory(s, Cache, Settings.Value, x, Address)))
				.Switch()
				.Subscribe(HistoryOrders, CloseCancellation.Token);

			FilterItems.Select(p => p.Changed()).Merge().Throttle(Consts.FilterUpdateTimeout, UiScheduler)
			.Select(_ => Filter())
			.Subscribe(Lines, CloseCancellation.Token);
		}

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);
			Attach(view as UIElement, ProductInfo.Bindings);
		}

		private IList<IOrderLine> Filter()
		{
			var selected = FilterItems.Where(p => p.IsSelected).Select(p => p.Item.Item1).ToArray();
			if (selected.Count() != FilterItems.Count())
			{
				var ids = new List<uint>();
				var lines = Source.OfType<OrderLine>();
				if (selected.Contains("InFrozenOrders"))
					ids.AddRange(lines.Where(x => x.InFrozenOrders).Select(x => x.Id));
				if (selected.Contains("IsMinCost"))
					ids.AddRange(lines.Where(x => x.IsMinCost).Select(x => x.Id));
				if (selected.Contains("IsNotMinCost"))
					ids.AddRange(lines.Where(x => !x.IsMinCost).Select(x => x.Id));
				if (selected.Contains("OnlyWarning"))
					ids.AddRange(lines.Where(x => x.SendResult != LineResultStatus.OK).Select(x => x.Id));
				return lines.Where(x => ids.Contains(x.Id)).OrderBy(l => l.ProducerSynonym).LinkTo(Source);
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
				sentLines.Each(l => l.Configure(User));
				Env.RxQuery(s => MatchedWaybills.GetLookUp(s, sentLines))
					.Subscribe(x => sentLines.Each(y => y.Configure(x)));
			}
			// Текущие заказы
			else
			{
				Order.Value.Lines.Each(l => l.Configure(User));
				Order.Value.Lines
					.Cast<OrderLine>()
					.Where(x => frozenProducts.Contains(x.ProductId))
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
	}
}
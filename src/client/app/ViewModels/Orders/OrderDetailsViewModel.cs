using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
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

namespace AnalitF.Net.Client.ViewModels.Orders
{
	public class OrderDetailsViewModel : BaseScreen, IPrintable
	{
		private uint orderId;
		private Type type;
		private Editor editor;

		public OrderDetailsViewModel(IOrder order)
		{
			orderId = order.Id;
			type = NHibernateUtil.GetClass(order);
			DisplayName = "Архивный заказ";

			OnlyWarning = new NotifyValue<bool>();
			Lines = new NotifyValue<IList<IOrderLine>>(new List<IOrderLine>(), Filter, OnlyWarning);
			CurrentLine = new NotifyValue<IOrderLine>();
			MatchedWaybills = new MatchedWaybills(this,
				CurrentLine.OfType<SentOrderLine>().ToValue(),
				new NotifyValue<bool>(!IsCurrentOrder));
			if (User.CanExport(this, type.Name))
				excelExporter.Properties = new []{ "Lines" };
			else
				excelExporter.Properties = new string[0];
			excelExporter.ActiveProperty.Refresh();
		}

		public IList<IOrderLine> Source { get; set; }

		public bool IsCurrentOrder
		{
			get { return type == typeof(Order); }
		}

		public NotifyValue<bool> OnlyWarning { get; set; }
		public NotifyValue<bool> OnlyWarningVisible { get; set; }

		public InlineEditWarning OrderWarning { get; set; }

		public ProductInfo ProductInfo { get; set; }

		public IOrder Order { get; set; }

		[Export]
		public NotifyValue<IList<IOrderLine>> Lines { get; set; }

		public NotifyValue<IOrderLine> CurrentLine { get; set; }

		public MatchedWaybills MatchedWaybills { get; set; }

		public bool CanPrint
		{
			get { return User.CanPrint<OrderDocument>(type); }
		}

		public bool ShowPriceVisible
		{
			get { return IsCurrentOrder; }
		}

		public bool CanShowPrice
		{
			get { return Order.Price != null && IsCurrentOrder; }
		}

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
			editor = new Editor(OrderWarning, Manager, currentOrderLine);
			CurrentLine
				.Subscribe(_ => {
					ProductInfo.CurrentOffer = (BaseOffer)CurrentLine.Value;
				});

			OnlyWarningVisible = new NotifyValue<bool>(User.IsPreprocessOrders && IsCurrentOrder);
			Lines.Subscribe(_ => editor.Lines = Lines.Value as IList);
		}

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);

			Attach(view, ProductInfo.Bindings);
		}

		private IList<IOrderLine> Filter()
		{
			if (OnlyWarning)
				return Source
					.OfType<OrderLine>().Where(l => l.SendResult != LineResultStatus.OK)
					.LinkTo(Source);

			return Source;
		}

		//todo init orderline
		public override void Update()
		{
			//Update - может быть вызван повторно если
			//мы вернулись на текущую форму с другой формы где были отредактированы данные
			if (Order != null)
				Session.Evict(Order);
			Order = (IOrder)Session.Get(type, orderId);
			//если заказ был удален
			if (Order == null) {
				IsSuccessfulActivated = false;
				return;
			}
			if (Settings.Value.HighlightUnmatchedOrderLines && !IsCurrentOrder) {
				var sentLines =  (IList<SentOrderLine>)Order.Lines;
				var lookup = MatchedWaybills.GetLookUp(StatelessSession, sentLines);
				sentLines.Each(l => l.Configure(User, lookup));
			}
			else {
				Order.Lines.Each(l => l.Configure(User));
			}


			Source = new ObservableCollection<IOrderLine>(Order.Lines);
			Source.ObservableForProperty(c => c.Count)
				.Where(e => e.Value == 0)
				.Subscribe(_ => {
					TryClose();
				});

			Lines.Recalculate();
		}

		public PrintResult Print()
		{
			return new PrintResult(DisplayName, new OrderDocument(Order).Build());
		}

		public void EnterLine()
		{
			ProductInfo.ShowCatalog();
		}

		public void ShowPrice()
		{
			if (!CanShowPrice)
				return;

			var offerViewModel = new PriceOfferViewModel(Order.Price.Id,
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
	}
}
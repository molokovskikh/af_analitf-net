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
using AnalitF.Net.Client.ViewModels.Parts;
using NHibernate;
using NPOI.SS.Formula.Functions;
using ReactiveUI;
using Address = AnalitF.Net.Client.Models.Address;

namespace AnalitF.Net.Client.ViewModels
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
			CurrentLine.Changed()
				.Subscribe(_ => {
					ProductInfo.CurrentOffer = (BaseOffer)CurrentLine.Value;
					editor.CurrentEdit = CurrentLine.Value as OrderLine;
				});
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

		public bool CanPrint
		{
			get { return User.CanPrint<OrderDocument>(type); }
		}

		public override bool CanExport
		{
			get { return User.CanExport(this, type.Name); }
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

			ProductInfo = new ProductInfo(StatelessSession, Manager, Shell);
			OrderWarning = new InlineEditWarning(UiScheduler, Manager);
			editor = new Editor(OrderWarning, Manager);
			OnlyWarningVisible = new NotifyValue<bool>(() => User.IsPreprocessOrders && IsCurrentOrder);

			editor.ObservableForProperty(e => e.CurrentEdit)
				.Select(e => e.Value)
				.BindTo(this, m => m.CurrentLine.Value);

			Lines.Changed()
				.Subscribe(_ => editor.Lines = Lines.Value as IList);
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
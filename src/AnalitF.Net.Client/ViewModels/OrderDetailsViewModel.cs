using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Parts;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class OrderDetailsViewModel : BaseScreen, IPrintable
	{
		private uint orderId;
		private Type type;
		private IOrderLine currentLine;
		private Editor editor;

		public OrderDetailsViewModel(IOrder order)
		{
			orderId = order.Id;
			type = order.GetType();
			DisplayName = "Архивный заказ";

			this.ObservableForProperty(m => m.CurrentLine)
				.Subscribe(_ => {
					ProductInfo.CurrentOffer = (BaseOffer)CurrentLine;
					editor.CurrentEdit = CurrentLine as OrderLine;
				});
			OrderWarning = new InlineEditWarning(UiScheduler, Manager);
			editor = new Editor(OrderWarning, Manager);
			editor.ObservableForProperty(e => e.CurrentEdit)
				.Select(e => e.Value)
				.BindTo(this, m => m.CurrentLine);
		}

		public bool IsCurrentOrder
		{
			get { return type == typeof(Order); }
		}

		public InlineEditWarning OrderWarning { get; set; }

		public ProductInfo ProductInfo { get; set; }

		public IOrder Order { get; set; }

		[Export]
		public IList<IOrderLine> Lines { get; set; }

		public IOrderLine CurrentLine
		{
			get { return currentLine; }
			set
			{
				if (currentLine == value)
					return;

				currentLine = value;
				NotifyOfPropertyChange("CurrentLine");
			}
		}

		public bool CanPrint
		{
			get { return true; }
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			ProductInfo = new ProductInfo(StatelessSession, Manager, Shell);
		}

		protected override void OnActivate()
		{
			base.OnActivate();

			Order = (IOrder)Session.Load(type, orderId);
			Lines = new ObservableCollection<IOrderLine>(Order.Lines);
			Lines.ObservableForProperty(c => c.Count)
				.Where(e => e.Value == 0)
				.Subscribe(_ => TryClose());

			if (IsCurrentOrder)
				editor.Lines = Lines as IList;
		}

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);

			Attach(view, ProductInfo.Bindings);
		}

		public PrintResult Print()
		{
			return new PrintResult(DisplayName, new OrderDocument(Order).Build());
		}

		public void EnterLine()
		{
			ProductInfo.ShowCatalog();
		}

		public bool ShowPriceVisible
		{
			get { return IsCurrentOrder; }
		}

		public bool CanShowPrice
		{
			get { return Order.Price != null && IsCurrentOrder; }
		}

		public void ShowPrice()
		{
			if (!CanShowPrice)
				return;

			var offerViewModel = new PriceOfferViewModel(Order.Price.Id, false);
			//временно не работает пока не придумаю решения по лучше
			//var offerId = ((OrderLine)CurrentLine).OfferId;
			//offerViewModel.CurrentOffer = offerViewModel.Offers.FirstOrDefault(o => o.Id == offerId);

			Shell.Navigate(offerViewModel);
		}

		public void Search(TextCompositionEventArgs args)
		{
			if (!CanShowPrice)
				return;

			var model = new SearchOfferViewModel();
			model.SearchText = args.Text;
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
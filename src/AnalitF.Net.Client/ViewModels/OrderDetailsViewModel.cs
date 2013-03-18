using System;
using System.Collections.Generic;
using System.Linq;
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

		public OrderDetailsViewModel(IOrder order)
		{
			orderId = order.Id;
			type = order.GetType();
			DisplayName = "Архивный заказ";

			this.ObservableForProperty(m => m.CurrentLine)
				.Subscribe(_ => ProductInfo.CurrentOffer = (BaseOffer)CurrentLine);
		}

		public ProductInfo ProductInfo { get; set; }

		public IOrder Order { get; set; }

		[Export]
		public IList<IOrderLine> Lines { get; set; }

		public IOrderLine CurrentLine
		{
			get { return currentLine; }
			set
			{
				currentLine = value;
				NotifyOfPropertyChange("CurrentLine");
			}
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
			Lines = Order.Lines.ToList();
		}

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);

			Attach(view, ProductInfo.Bindings);
		}

		public bool CanPrint
		{
			get { return true; }
		}

		public PrintResult Print()
		{
			return new PrintResult(new OrderDocument(Order).Build(), DisplayName);
		}
	}
}
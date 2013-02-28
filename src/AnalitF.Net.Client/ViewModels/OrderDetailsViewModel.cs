using System.Collections.Generic;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;

namespace AnalitF.Net.Client.ViewModels
{
	public class OrderDetailsViewModel : BaseScreen, IPrintable
	{
		public OrderDetailsViewModel(Order order)
		{
			Order = order;
			Lines = order.Lines;
			DisplayName = "Архивный заказ";
		}

		public Order Order { get; set; }

		[Export]
		public IList<OrderLine> Lines { get; set; }

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
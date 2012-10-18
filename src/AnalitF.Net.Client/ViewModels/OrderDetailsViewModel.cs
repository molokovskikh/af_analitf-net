using System.Collections.Generic;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.ViewModels
{
	public class OrderDetailsViewModel : BaseScreen
	{
		public OrderDetailsViewModel(Order order)
		{
			Order = order;
			Lines = order.Lines;
			DisplayName = "Архивный заказ";
		}

		public Order Order { get; set; }

		public IList<OrderLine> Lines { get; set; }
	}
}
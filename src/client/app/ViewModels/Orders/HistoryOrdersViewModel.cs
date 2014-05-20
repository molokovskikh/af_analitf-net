using System.Collections.Generic;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Orders
{
	public class HistoryOrdersViewModel : Screen
	{
		public HistoryOrdersViewModel(Catalog catalog, Offer offer, List<SentOrderLine> lines)
		{
			Offer = offer;
			Catalog = catalog;
			Lines = lines;
			DisplayName = "Предыдущие заказы";
		}

		public Offer Offer { get; set; }

		public Catalog Catalog { get; set; }

		public List<SentOrderLine> Lines { get; set; }
	}
}
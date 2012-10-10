using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class OrderLinesViewModel : BaseScreen
	{
		public OrderLinesViewModel()
		{
			DisplayName = "Сводный заказ";
			Lines = Session.Query<OrderLine>()
				.OrderBy(l => l.ProductSynonym)
				.ThenBy(l => l.ProducerSynonym)
				.ToList();
		}

		public virtual List<Price> Prices { get; set; }
		public virtual List<OrderLine> Lines { get; set; }

		public virtual decimal Sum
		{
			get
			{
				return Lines.Sum(l => l.Sum);
			}
		}

		public void Delete()
		{

		}

		public void ShowCatalog()
		{

		}

		public void ShowCatalogWithMnnFilter()
		{

		}
	}
}
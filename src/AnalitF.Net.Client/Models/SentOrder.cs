using System;
using System.Collections.Generic;
using System.Linq;

namespace AnalitF.Net.Client.Models
{
	public class SentOrder
	{
		public SentOrder()
		{}

		public SentOrder(Order order)
		{
			PriceName = order.Price.Name;
			CreatedOn = order.CreatedOn;
			SentOn = DateTime.Now;

			Lines = order.Lines
				.Select(l => new SentOrderLine(this, l))
				.ToList();
		}

		public virtual uint Id { get; set; }

		public virtual string PriceName { get; set; }

		public virtual DateTime CreatedOn { get; set; }

		public virtual DateTime SentOn { get; set; }

		public virtual IList<SentOrderLine> Lines { get; set; }
	}
}
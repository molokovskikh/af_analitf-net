using System.Collections.Generic;

namespace AnalitF.Net.Client.Models
{
	public class Order
	{
		public Order()
		{
			Lines = new List<OrderLine>();
		}

		public Order(Price price)
			: this()
		{
			Price = price;
		}

		public virtual uint Id { get; set; }

		public virtual Price Price { get; set; }

		public virtual IList<OrderLine> Lines { get; set; }

		public virtual bool IsEmpty
		{
			get { return Lines.Count == 0; }
		}

		public virtual void RemoveLine(OrderLine line)
		{
			Lines.Remove(line);
		}

		public virtual void AddLine(OrderLine line)
		{
			Lines.Add(line);
		}
	}
}
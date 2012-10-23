namespace AnalitF.Net.Client.Models
{
	public class SentOrderLine : BaseOffer
	{
		public SentOrderLine()
		{}

		public SentOrderLine(SentOrder order, OrderLine orderLine)
			: base(orderLine)
		{
			Order = order;
			Count = orderLine.Count;
		}

		public virtual uint Id { get; set; }

		public virtual uint Count { get; set; }

		public virtual SentOrder Order { get; set; }
	}
}
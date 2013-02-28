namespace AnalitF.Net.Client.Models
{
	public class SentOrderLine : BaseOffer, IOrderLine
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

		public virtual decimal Sum
		{
			get { return Count * Cost; }
		}

		public virtual SentOrder Order { get; set; }
	}
}
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
			Comment = orderLine.Comment;
			ResultCost = orderLine.ResultCost;
		}

		public virtual uint Id { get; set; }

		public virtual uint Count { get; set; }

		public virtual string Comment { get; set; }

		public virtual decimal ResultCost { get; set; }

		public virtual decimal ResultSum
		{
			get { return Count * ResultCost; }
		}

		public virtual SentOrder Order { get; set; }
	}
}
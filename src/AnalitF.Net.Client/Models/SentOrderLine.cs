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

		public virtual decimal Sum
		{
			get { return Count * Cost; }
		}

		public virtual decimal MixedSum
		{
			get { return Sum; }
		}

		public virtual SentOrder Order { get; set; }

		public override decimal GetResultCost()
		{
			return ResultCost;
		}
	}
}
namespace AnalitF.Net.Client.Models
{
	public class DeletedOrderLine : BaseOffer, IOrderLine
	{
		public DeletedOrderLine()
		{
		}

		public DeletedOrderLine(DeletedOrder order, OrderLine orderLine)
			: base(orderLine)
		{
			Order = order;
			ServerOrderId = order.ServerId;
			Count = orderLine.Count;
			Comment = orderLine.Comment;
			ResultCost = orderLine.ResultCost;
			ServerId = orderLine.ExportId;
			RetailCost = orderLine.RetailCost;
			RetailMarkup = orderLine.RetailMarkup;
			IsEditByUser = orderLine.IsEditByUser;
		}

		public virtual uint Id { get; set; }

		public virtual uint? ServerId { get; set; }

		public virtual ulong? ServerOrderId { get; set; }

		public virtual uint Count { get; set; }

		public virtual string Comment { get; set; }

		public virtual decimal ResultCost { get; set; }

		public virtual bool IsEditByUser { get; set; }

		public virtual decimal? RetailCost { get; set; }

		public virtual decimal? RetailMarkup { get; set; }

		public virtual decimal MixedCost => HideCost ? ResultCost : Cost;

		public virtual decimal Sum => Count * Cost;

		public virtual decimal MixedSum => Count * MixedCost;

		public virtual DeletedOrder Order { get; set; }

		public override decimal GetResultCost()
		{
			return ResultCost;
		}
	}
}
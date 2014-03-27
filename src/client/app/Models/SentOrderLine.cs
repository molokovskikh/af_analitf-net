using AnalitF.Net.Client.Config.Initializers;

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

		public virtual decimal MixedCost
		{
			get
			{
				return HideCost ? ResultCost : Cost;
			}
		}

		public virtual decimal Sum
		{
			get { return Count * Cost; }
		}

		public virtual decimal MixedSum
		{
			get { return Sum; }
		}

		public virtual SentOrder Order { get; set; }

		//заглушка, для поля редактирования на форме детализации заказа
		//хоть поле которое содержит ошибку скрыто для отправленных заказов
		//но биндинг все равно будет ругаться если поля не будет
		[Ignore]
		public virtual string LongSendError { get; set; }

		public override decimal GetResultCost()
		{
			return ResultCost;
		}
	}
}
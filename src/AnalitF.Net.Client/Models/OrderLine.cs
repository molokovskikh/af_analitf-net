namespace AnalitF.Net.Client.Models
{
	public class OrderLine
	{
		public OrderLine()
		{
		}

		public OrderLine(Order order, Offer currentOffer)
		{
			Order = order;
		}

		public virtual uint Id { get; set; }

		public virtual Order Order { get; set; }

		public virtual string ProductSynonym { get; set; }

		public virtual string ProducerSynonym { get; set; }

		public virtual uint Quantity { get; set; }

		public virtual decimal Cost { get; set; }

		public virtual uint Count { get; set; }

		public virtual decimal Sum
		{
			get { return Count * Cost; }
		}
	}
}
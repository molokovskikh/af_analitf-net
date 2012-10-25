namespace AnalitF.Net.Client.Models
{
	public class OrderLine : BaseOffer
	{
		public OrderLine()
		{
		}

		public OrderLine(Order order, Offer offer, uint count)
			: base(offer)
		{
			Order = order;
			OfferId = offer.Id;
			Count = count;
		}

		public virtual uint Id { get; set; }

		public virtual Order Order { get; set; }

		public virtual uint Count { get; set; }

		public virtual ulong OfferId { get; set; }

		public virtual decimal Sum
		{
			get { return Count * Cost; }
		}
	}
}
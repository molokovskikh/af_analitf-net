namespace AnalitF.Net.Client.Models
{
	public class MaxProducerCost
	{
		public virtual ulong Id { get; set; }

		public virtual uint ProductId { get; set; }

		public virtual uint CatalogId { get; set; }

		public virtual uint? ProducerId { get; set; }

		public virtual string Product { get; set; }

		public virtual string Producer { get; set; }

		public virtual decimal Cost { get; set; }
	}
}
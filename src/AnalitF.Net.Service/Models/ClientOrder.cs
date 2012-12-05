using System;
using Common.Models;

namespace AnalitF.Net.Models
{
	public class ClientOrder
	{
		public uint ClientOrderId { get; set; }
		public uint PriceId { get; set; }
		public uint AddressId { get; set; }
		public ulong RegionId { get; set; }
		public DateTime CreatedOn { get; set; }
		public DateTime PriceDate { get; set; }
		public string Comment { get; set; }

		public ClientOrderItem[] Items { get; set; }
	}

	public class ClientOrderItem : BaseOffer
	{
		public ulong OfferId { get; set; }
		public uint Count { get; set; }
		public uint? ProducerId { get; set; }
		public decimal Cost { get; set; }
	}
}
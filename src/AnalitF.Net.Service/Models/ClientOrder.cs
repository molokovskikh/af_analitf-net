using System;
using Common.Models;

namespace AnalitF.Net.Service.Models
{
	public class SyncRequest
	{
		public PriceSettings[] Prices;
		public ClientOrder[] Orders;
	}

	public class PriceSettings
	{
		public uint PriceId;
		public uint RegionId;
		public bool Active;
	}

	public class ClientOrder
	{
		public uint ClientOrderId;
		public uint PriceId;
		public uint AddressId;
		public ulong RegionId;
		public DateTime CreatedOn;
		public DateTime PriceDate;
		public string Comment;

		public ClientOrderItem[] Items;
	}

	public class PostOrderResult
	{
		public uint ClientOrderId;
		public ulong ServerOrderId;
		public string Error;

		public PostOrderResult()
		{
		}

		public PostOrderResult(uint? clientOrderId, string error)
		{
			ClientOrderId = clientOrderId.GetValueOrDefault();
			Error = error;
		}

		public PostOrderResult(Order order)
		{
			ClientOrderId = order.ClientOrderId.GetValueOrDefault();
			ServerOrderId = order.RowId;
		}
	}

	public class OfferComposedId
	{
		public ulong RegionId { get; set; }
		public ulong OfferId { get; set; }
	}

	public class ClientOrderItem : BaseOffer
	{
		public OfferComposedId OfferId { get; set; }
		public uint Count { get; set; }
		public uint? ProducerId { get; set; }
		public decimal Cost { get; set; }
	}
}
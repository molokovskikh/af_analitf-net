using System;
using System.Collections.Generic;
#if SERVER
using Common.Models;
namespace AnalitF.Net.Service.Models
{
#endif
#if CLIENT
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Models
{
#endif
#if CLIENT
	public class ClientOrder
	{
		public uint ClientOrderId;
		public uint PriceId;
		public uint AddressId;
		public ulong RegionId;
		public DateTime CreatedOn;
		public DateTime PriceDate;
		public string Comment;

		public OrderLine[] Items;
	}
#endif
	public class SyncRequest
	{
		public PriceSettings[] Prices;
		public ClientOrder[] Orders;

		public bool Force;

		public SyncRequest()
		{
		}

		public SyncRequest(ClientOrder[] orders, bool force)
		{
			Orders = orders;
			Force = force;
		}

		public SyncRequest(PriceSettings[] prices)
		{
			Prices = prices;
		}
	}

	public class PriceSettings
	{
		public uint PriceId;
		public ulong RegionId;
		public bool Active;

		public PriceSettings(uint priceId, ulong regionId, bool active)
		{
			PriceId = priceId;
			RegionId = regionId;
			Active = active;
		}
	}

	[Flags]
	public enum LineResultStatus
	{
		OK = 0,
		CostChanged = 1,
		QuantityChanged = 2,
		NoOffers = 4
	}

	public enum OrderResultStatus
	{
		OK,
		Warning,
		Reject
	}

	public class OrderLineResult
	{
		public uint ClientLineId;
		public uint? ServerQuantity;
		public decimal? ServerCost;
		public LineResultStatus Result;

		public OrderLineResult()
		{
		}

		public OrderLineResult(uint id)
		{
			ClientLineId = id;
		}
	}

	public class OrderResult
	{
		public uint ClientOrderId;
		public ulong ServerOrderId;
		public string Error;
		public OrderResultStatus Result;
		public OrderLineResult[] Lines = new OrderLineResult[0];

		public OrderResult()
		{
		}

		public OrderResult(uint? clientOrderId, string error)
		{
			ClientOrderId = clientOrderId.GetValueOrDefault();
			Error = error;
			Result = OrderResultStatus.Reject;
		}

#if SERVER
		public OrderResult(Order order)
		{
			ClientOrderId = order.ClientOrderId.GetValueOrDefault();
			ServerOrderId = order.RowId;
		}
#endif

		public OrderResult(uint? clientOrderId, string error, List<OrderLineResult> lines)
		{
			ClientOrderId = clientOrderId.GetValueOrDefault();
			Error = error;
			Result = OrderResultStatus.Warning;
			Lines = lines.ToArray();
		}
	}
#if SERVER

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

	public class OfferComposedId
	{
		public ulong RegionId;
		public ulong OfferId;
	}

	public class ClientOrderItem : BaseOffer
	{
		public uint Id;
		public OfferComposedId OfferId;
		public uint Count;
		public uint? ProducerId;
		public decimal Cost;
	}
#endif
}
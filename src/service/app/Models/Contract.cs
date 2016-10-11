using System;
using System.Collections.Generic;
using Common.Tools;
using System.Linq;
#if SERVER
using Common.Models;
namespace AnalitF.Net.Service.Models
#endif
#if CLIENT
using AnalitF.Net.Client.Models;
namespace AnalitF.Net.Client.Models
#endif
{
	public class ClientOrder
	{
		public uint ClientOrderId;
		public uint PriceId;
		public string PriceName;
		public uint? CostId;
		public string CostName;
		public uint AddressId;
		public ulong RegionId;
		public DateTime CreatedOn;
		public DateTime PriceDate;
		public string Comment;
#if SERVER
		public ClientOrderItem[] Items;
#endif
#if CLIENT
		public OrderLine[] Items;
#endif
	}

	public class HistoryRequest
	{
		public uint[] WaybillIds;
		public ulong[] OrderIds;
		public bool IgnoreOrders;
		public bool IgnoreWaybills;
	}

	public class FeedbackMessage
	{
		public bool IsBilling;
		public bool IsSupport;
		public bool IsOffice;
		public string Subject;
		public string Body;
		public byte[] Attachments;
	}

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

		public override string ToString()
		{
			return string.Format("Prices: ({0})", Prices == null ? "null" : Prices.Implode());
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

		public override string ToString()
		{
			return string.Format("PriceId: {0}, RegionId: {1}, Active: {2}", PriceId, RegionId, Active);
		}
	}

	[Flags]
	public enum LineResultStatus
	{
		OK = 0,
		CostChanged = 1,
		QuantityChanged = 2,
		NoOffers = 4,
		CountReduced = 8,
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
		public uint? ServerLineId;
		public uint? ServerQuantity;
		public decimal? ServerCost;
		public LineResultStatus Result;

		public OrderLineResult()
		{
		}

		public OrderLineResult(uint clientId, uint serverId)
		{
			ClientLineId = clientId;
			ServerLineId = serverId;
			Result = LineResultStatus.OK;
		}

		public OrderLineResult(uint id)
		{
			ClientLineId = id;
		}
	}

	public class ConfirmRequest
	{
		public uint RequestId;
		public string Message;

		public ConfirmRequest(uint requestId, string message = null)
		{
			RequestId = requestId;
			Message = message;
		}
	}

	public class OrderResult
	{
		public uint ClientOrderId;
		public ulong ServerOrderId;
		public string Error;
		public OrderResultStatus Result = OrderResultStatus.OK;
		public List<OrderLineResult> Lines = new List<OrderLineResult>();

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
		public OrderResult(Order order, Dictionary<OrderItem, uint> map)
		{
			ClientOrderId = order.ClientOrderId.GetValueOrDefault();
			ServerOrderId = order.RowId;
			Lines = order.OrderItems.Select(i => new OrderLineResult(map.GetValueOrDefault(i), i.RowId)).ToList();
		}
#endif

		public OrderResult(uint? clientOrderId, string error, List<OrderLineResult> lines)
		{
			ClientOrderId = clientOrderId.GetValueOrDefault();
			Error = error;
			Result = OrderResultStatus.Warning;
			Lines = lines.ToList();
		}

		public override string ToString()
		{
			return Error;
		}
	}
#if SERVER

	public class OfferComposedId
	{
		public ulong RegionId;
		public ulong OfferId;
	}

	public class PriceComposedId
	{
		public uint PriceId;
		public ulong RegionId;
	}

	public class ClientOrderItem : BaseOffer
	{
		public uint Id;
		public OfferComposedId OfferId;
		public uint Count;
		public uint? ProducerId;
		public decimal Cost;
		public decimal? ResultCost;

		public decimal? LeaderCost;
		public PriceComposedId LeaderPrice;
		public decimal? MinCost;
		public PriceComposedId MinPrice;

		public bool OriginalJunk;
	}
#endif

	public class BatchItem
	{
		public string Code;

		public string CodeCr;

		public string ProductName;

		public string ProducerName;

		public uint Quantity;

		public string SupplierDeliveryId;

		public Dictionary<string, string> ServiceValues;

		public string Priority;

		public float? BaseCost;
	}

	public class BatchRequest
	{
		public DateTime? LastSync;
		public uint AddressId;
		public int JunkPeriod;
		public List<BatchItem> BatchItems;

		public BatchRequest()
		{
			BatchItems = new List<BatchItem>();
		}

		public BatchRequest(uint addressId, int junkPeriod, DateTime? lastSync)
			: this()
		{
			AddressId = addressId;
			LastSync = lastSync;
			JunkPeriod = junkPeriod;
		}
	}

	public enum ActionType
	{
		Sale,
		Stock,
		ReturnToSupplier,
		CancelReturnToSupplier,
		InventoryDoc,
		CancelInventoryDoc,
		CheckReturn
	}

	public class StockActionAttrs
	{
		public virtual ActionType ActionType { get; set; }
		public virtual uint ClientStockId { get; set; }
		public virtual ulong? SourceStockId { get; set; }
		public virtual int? SourceStockVersion { get; set; }
		public virtual decimal Quantity { get; set; }
		public virtual decimal? RetailCost { get; set; }
		public virtual decimal? RetailMarkup { get; set; }
	}
}
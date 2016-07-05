using System.ComponentModel;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Models.Inventory
{
	public enum DetailStatus
	{
		[Description("Новый")] New,
		[Description("Принят")] Received,
	}

	public class ReceivingDetail
	{
		public virtual uint Id { get; set; }
		public virtual string Product { get; set; }
		public virtual string Producer { get; set; }
		public virtual decimal RetailCost { get; set; }
		public virtual DetailStatus Status { get; set; }
		public virtual decimal Cost { get; set; }
		public virtual decimal Count { get; set; }
		public virtual decimal Sum => Count * Cost;
		public virtual uint ReceivingOrderId { get; set; }
		public virtual uint LineId { get; set; }

		public virtual string StatusName => DescriptionHelper.GetDescription(Status);

		public virtual Stock ToStock()
		{
			Status = DetailStatus.Received;
			return new Stock {
				Product = Product,
				Producer = Producer,
				Count = Count,
				Cost = Cost,
				RetailCost = RetailCost,
				Status = StockStatus.Available,
			};
		}
	}
}
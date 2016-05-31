using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Models.Inventory
{
	public enum StockStatus
	{
		Available,
		Reciving,
	}

	public class Stock
	{
		public virtual uint Id { get; set; }

		public virtual StockStatus Status { get; set; }

		public virtual uint? ReceivingOrderId { get; set; }

		public virtual string Product { get; set; }
		public virtual string Producer { get; set; }
		public virtual decimal RetailCost { get; set; }
		public virtual decimal Count { get; set; }
	}
}
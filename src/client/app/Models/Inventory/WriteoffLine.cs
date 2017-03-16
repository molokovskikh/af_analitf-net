using System;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class WriteoffLine : BaseStock
	{
		public WriteoffLine()
		{
		}

		public WriteoffLine(Stock stock, decimal quantity)
		{
			Stock.Copy(stock, this);
			Id = 0;
			WaybillLineId = stock.WaybillLineId;
			Stock = stock;
			Quantity = quantity;
			Stock.Reserve(Quantity);
		}

		public virtual uint Id { get; set; }

		public virtual uint? ServerDocId { get; set; }

		public virtual uint? WaybillLineId { get; set; }

		public virtual decimal? SupplierSumWithoutNds => SupplierCostWithoutNds*Quantity;

		public virtual decimal? SupplierSum => SupplierCost * Quantity;

		public virtual decimal? RetailSum => RetailCost*Quantity;

		public virtual decimal Quantity { get; set; }

		public virtual Stock Stock { get; set; }

		public virtual void UpdateQuantity(decimal quantity)
		{
			Stock.Release(Quantity);
			Stock.Reserve(quantity);
			Quantity = quantity;
		}
	}
}
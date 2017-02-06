using System;
using System.Linq;
using AnalitF.Net.Client.Config.NHibernate;
using Common.Tools;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class ReturnLine : BaseStock
	{
		public virtual uint Id { get; set; }

		public virtual uint? WaybillLineId { get; set; }

		public virtual decimal Quantity { get; set; }

		public virtual decimal SupplierSumWithoutNds => Quantity * SupplierCostWithoutNds.GetValueOrDefault();

		public virtual decimal SupplierSum => Quantity * SupplierCost.GetValueOrDefault();

		public virtual decimal RetailSum => Quantity * RetailCost.GetValueOrDefault();

		public virtual Stock Stock { get; set; }

		public ReturnLine()
		{

		}

		public ReturnLine(Stock stock, decimal quantity)
		{
			Stock.Copy(stock, this);
			Id = 0;
			WaybillLineId = stock.WaybillLineId;
			Stock = stock;
			Quantity = quantity;
			Stock.Reserve(Quantity);
		}

		public virtual void UpdateQuantity(decimal quantity)
		{
			Stock.Release(Quantity);
			Stock.Reserve(quantity);
			Quantity = quantity;
		}
	}
}

using System;
using System.Linq;
using AnalitF.Net.Client.Config.NHibernate;
using Common.Tools;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class DisplacementLine : BaseStock
	{
		public virtual uint Id { get; set; }

		public virtual uint? WaybillLineId { get; set; }

		public virtual decimal Quantity { get; set; }

		public virtual decimal SupplierSumWithoutNds => Quantity * SupplierCostWithoutNds.GetValueOrDefault();

		public virtual decimal SupplierSum => Quantity * SupplierCost.GetValueOrDefault();

		public virtual decimal RetailSum => Quantity * RetailCost.GetValueOrDefault();

		public virtual string Period { get; set; }

		public virtual Stock SrcStock { get; set; }

		public virtual Stock DstStock { get; set; }

		public DisplacementLine()
		{

		}

		public DisplacementLine(Stock srcStock, Stock dstStock, decimal quantity)
		{
			Stock.Copy(srcStock, this);
			Id = 0;
			WaybillLineId = srcStock.WaybillLineId;
			SrcStock = srcStock;
			DstStock = dstStock;
			Quantity = quantity;
			SrcStock.Reserve(Quantity);
		}

		public virtual void UpdateQuantity(decimal quantity)
		{
			SrcStock.Release(Quantity);
			SrcStock.Reserve(quantity);
			Quantity = quantity;
		}
	}
}

using System;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class UnpackingLine : BaseStock
	{
		public UnpackingLine()
		{
		}

		public UnpackingLine(Stock srcStock, int multiplicity)
		{
			// распаковывается одна упаковка
			var quantity = 1m;
			Stock.Copy(srcStock, this);
			var dstStock = srcStock.Copy();
			dstStock.Quantity = 0;
			dstStock.Unpacked = true;
			Quantity = dstStock.ReservedQuantity = dstStock.Multiplicity = multiplicity;
			DstStock = dstStock;

			Id = 0;
			SrcQuantity = quantity;
			SrcRetailCost = srcStock.RetailCost;
			srcStock.Reserve(quantity);
			SrcStock = srcStock;

			if (srcStock.RetailCost.HasValue)
				RetailCost = dstStock.RetailCost = getPriceForUnit(srcStock.RetailCost.Value, multiplicity);

			if (srcStock.SupplierCost.HasValue)
				dstStock.SupplierCost = getPriceForUnit(srcStock.SupplierCost.Value, multiplicity);
		}

		public UnpackingLine(Stock srcStock, Stock dstStock)
		{
			DstStock = dstStock;
			SrcQuantity = 1;
			SrcRetailCost = srcStock.RetailCost;
			srcStock.Reserve(1);
			SrcStock = srcStock;
			
		}


		public virtual uint Id { get; set; }

		public virtual uint? ServerDocId { get; set; }

		public virtual decimal Quantity { get; set; }

		public override decimal? RetailCost { get; set; }

		public virtual decimal? RetailSum => RetailCost * Quantity;

		public virtual decimal SrcQuantity { get; set; }

		public virtual decimal? SrcRetailCost { get; set; }

		public virtual decimal? SrcRetailSum => SrcRetailCost * SrcQuantity;

		public virtual decimal? Delta => RetailSum - SrcRetailSum;

		// признак движения распакованного товара
		public virtual bool Moved => (DstStock.Quantity + DstStock.ReservedQuantity) != DstStock.Multiplicity;

		public virtual Stock SrcStock { get; set; }
		public virtual Stock DstStock { get; set; }

		private decimal getPriceForUnit(decimal price, int multiplicity)
		{
			return Math.Floor(price * 100 / multiplicity) / 100;
		}
	}
}
using System;
using System.ComponentModel;
using NHibernate;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class InventoryLine : BaseStock
	{
		public InventoryLine()
		{
		}

		public InventoryLine(InventoryDoc doc, Stock stock, decimal quantity, ISession session, bool stockIsNew = false)
		{
			Stock.Copy(stock, this);
			Id = 0;
			Stock = stock;
			Quantity = quantity;
			StockIsNew = stockIsNew;
			Doc = doc;
			if (stockIsNew)
			{
				Stock.ReservedQuantity += quantity;
				session.Save(new StockAction(ActionType.Stock, ActionTypeChange.Plus, Stock, Doc, Quantity));
			}
			else
				session.Save(Stock.InventoryDoc(Doc, quantity));
		}

		public virtual uint Id { get; set; }

		public virtual InventoryDoc Doc { get; set; }

		public virtual uint? ServerDocId { get; set; }

		public virtual decimal SupplierSumWithoutNds => Quantity * SupplierCostWithoutNds.GetValueOrDefault();

		public virtual decimal SupplierSum => Quantity * SupplierCost.GetValueOrDefault();

		public virtual decimal RetailSum => Quantity * RetailCost.GetValueOrDefault();

		public virtual decimal Quantity { get; set; }

		public virtual Stock Stock { get; set; }

		// признак, что соотв сток был создан при создании этой строки, иначе сток уже был
		public virtual bool StockIsNew { get; set; }

		public virtual void UpdateQuantity(decimal oldQuantity, ISession session)
		{
			// с поставки наружу
			session.Save(Stock.CancelInventoryDoc(Doc, oldQuantity));
			// снаружи в поставку
			session.Save(Stock.InventoryDoc(Doc, Quantity));
		}
	}
}
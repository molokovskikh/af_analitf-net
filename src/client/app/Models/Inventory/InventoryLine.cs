using System;
using System.ComponentModel;
using NHibernate;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class InventoryLine : BaseStock, IEditableObject
	{
		public InventoryLine()
		{
		}

		public InventoryLine(Stock stock, decimal quantity, ISession session)
		{
			Stock.Copy(stock, this);
			Id = 0;
			Stock = stock;
			Quantity = quantity;
			session.Save(Stock.InventoryDoc(quantity));
		}

		public virtual uint Id { get; set; }

		public virtual decimal SupplierSumWithoutNds => Quantity * SupplierCostWithoutNds.GetValueOrDefault();

		public virtual decimal SupplierSum => Quantity * SupplierCost.GetValueOrDefault();

		public virtual decimal RetailSum => Quantity * RetailCost.GetValueOrDefault();

		public virtual decimal Quantity { get; set; }

		public virtual string Period { get; set; }

		public virtual Stock Stock { get; set; }

		public virtual void UpdateQuantity(decimal oldQuantity, ISession session)
		{
			// с поставки наружу
			session.Save(Stock.CancelInventoryDoc(oldQuantity));
			// снаружи в поставку
			session.Save(Stock.InventoryDoc(Quantity));
		}

		public virtual void BeginEdit()
		{
		}

		public virtual void EndEdit()
		{
		}

		public virtual void CancelEdit()
		{
		}
	}
}
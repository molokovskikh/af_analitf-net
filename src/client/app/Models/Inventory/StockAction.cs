using System;
using AnalitF.Net.Client.Config.NHibernate;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class StockAction : StockActionAttrs
	{
		public StockAction()
		{
		}

		public StockAction(ActionType action, Stock stock, decimal quantity)
		{
			ActionType = action;
			ClientStockId = stock.Id;
			SourceStockId = stock.ServerId;
			SourceStockVersion = stock.ServerVersion;
			Quantity = quantity;
			SrcStock = stock;
			Timestamp = DateTime.Now;
		}

		public virtual uint Id { get; set; }
		public virtual DateTime Timestamp { get; set; }

		[Ignore]
		public virtual Stock SrcStock { get; set; }
	}
}
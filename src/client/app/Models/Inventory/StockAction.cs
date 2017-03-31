using System;
using AnalitF.Net.Client.Config.NHibernate;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class StockAction : StockActionAttrs
	{
		public StockAction()
		{
		}

		public StockAction(ActionType action, ActionTypeChange typechange, Stock stock,
			IStockDocument doc, decimal quantity, decimal? discountsum = null)
		{
			ActionType = action;
			TypeChange = typechange;
			ClientStockId = stock.Id;
			SourceStockId = stock.ServerId;
			SourceStockVersion = stock.ServerVersion;
			Quantity = quantity;
			SrcStock = stock;
			Timestamp = DateTime.Now;
			DisplayDoc = doc.DisplayName;
			NumberDoc = doc.NumberDoc;
			FromIn = doc.FromIn;
			OutTo = doc.OutTo;
			RetailCost = stock.RetailCost;
			RetailMarkup = stock.RetailMarkup;
			DiscountSum = TypeChange == ActionTypeChange.Minus
						&& discountsum != null ? -discountsum : discountsum;
		}

		public virtual uint Id { get; set; }
		[Ignore]
		public virtual string Document
		{
			get
			{
				return DisplayDoc + " " + NumberDoc;
			}
		}

		[Ignore]
		public virtual decimal QuantityEx
		{
			get
			{
				if (TypeChange == ActionTypeChange.Minus)
					return -Quantity;
				else
					return Quantity;
			}
		}

		[Ignore]
		public virtual decimal? RetailCostEx
		{
			get
			{
				if (TypeChange == ActionTypeChange.Minus && RetailCost != null)
					return -RetailCost;
				else
					return RetailCost;
			}
		}

		[Ignore]
		public virtual decimal? RetailSumm
		{
			get
			{
				if (RetailCost == null)
					return null;
				else
					return Math.Round((RetailCost * Quantity - (DiscountSum != null ? DiscountSum : 0)).Value, 2);
			}
		}

		[Ignore]
		public virtual decimal? RetailSummEx
		{
			get
			{
				if (TypeChange == ActionTypeChange.Minus && RetailSumm != null)
					return -RetailSumm;
				else
					return RetailSumm;
			}
		}

		[Ignore]
		public virtual Stock DstStock { get; set; }

		[Ignore]
		public virtual Stock SrcStock { get; set; }
	}
}
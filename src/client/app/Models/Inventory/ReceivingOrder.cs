using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Views.Inventory;
using Common.NHibernate;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Models.Inventory
{
	public enum ReceiveStatus
	{
		[Description("Новый")] New,
		[Description("В обработке")] InProgress,
		[Description("Закрыт")] Closed,
	}

	public class ReceivingOrder
	{
		public ReceivingOrder()
		{
			Lines = new List<ReceivingLine>();
		}

		public ReceivingOrder(Waybill waybill)
			: this()
		{
			Date = DateTime.Now;
			Supplier = waybill.Supplier;
			Address = waybill.Address;
			WaybillDate = waybill.DocumentDate;
			WaybillId = waybill.Id;
			UpdateStat(Lines);
		}

		public virtual uint Id { get; set; }
		public virtual DateTime Date { get; set; }
		public virtual Supplier Supplier { get; set; }
		public virtual Address Address { get; set; }

		public virtual uint? WaybillId { get; set; }
		public virtual DateTime? WaybillDate { get; set; }

		public virtual decimal Sum { get; set; }
		public virtual decimal RetailSum { get; set; }
		public virtual int LineCount { get; set; }

		public virtual IList<ReceivingLine> Lines { get; set; }

		public virtual void UpdateStat(IEnumerable<ReceivingLine> lines)
		{
			LineCount = lines.Count();
			Sum = lines.Sum(x => x.Sum);
			RetailSum = lines.Sum(x => x.RetailCost).GetValueOrDefault();
		}

		public virtual Stock[] ToStocks()
		{
			return Lines.Select(x => new Stock(this, x)).ToArray();
		}

		public static bool StockWaybill(ISession session, Waybill waybill)
		{
			List<StockAction> stockActions;
			var order = Create(waybill, out stockActions);
			if (order.Lines.Count == 0)
				return false;
			if (order.Lines.Count > 0)
				session.Save(order);
			foreach (var action in stockActions) {
				action.DstStock.ReceivingOrderId = order.Id;
				session.Save(action.DstStock);
				session.Update(action.SrcStock);
				action.ClientStockId = action.DstStock.Id;
			}
			session.SaveEach(stockActions);
			return true;
		}

		private static ReceivingOrder Create(Waybill waybill, out List<StockAction> stockActions)
		{
			var order = new ReceivingOrder(waybill);
			var lines = waybill.Lines.Where(x => x.IsReadyForStock && x.QuantityToReceive > 0).ToArray();
			stockActions = new List<StockAction>();
			foreach (var line in lines) {
				var receivingLine = new ReceivingLine(line);
				order.Lines.Add(receivingLine);
				line.Stock.Quantity -= line.QuantityToReceive;
				line.QuantityToReceive = 0;
				var stock = new Stock(order, receivingLine);
				stockActions.Add(new StockAction {
					ActionType = ActionType.Stock,
					SourceStockId = line.StockId,
					SourceStockVersion = line.StockVersion,
					Quantity = receivingLine.Quantity,
					RetailCost = receivingLine.RetailCost,
					RetailMarkup = receivingLine.RetailMarkup,
					DstStock = stock,
					SrcStock = line.Stock,
				});
			}
			order.UpdateStat(order.Lines);
			return order;
		}
	}
}
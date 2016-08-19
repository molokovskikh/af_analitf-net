using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Views.Inventory;
using Common.NHibernate;
using Common.Tools;
using NHibernate;

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
			var lines = waybill.Lines.Where(x => x.IsReadyForStock && x.QuantityToReceive > 0).ToArray();
			foreach (var line in lines) {
				Lines.Add(new ReceivingLine {
						Product = line.Product,
						ProductId = line.ProductId,
						Producer = line.Producer,
						ProducerId = line.ProductId,
						Quantity = line.QuantityToReceive,
						Cost = line.SupplierCost.GetValueOrDefault(),
						RetailCost = line.RetailCost.GetValueOrDefault(),
				});
				line.ReceivedQuantity += line.QuantityToReceive;
			}

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
			RetailSum = lines.Sum(x => x.RetailCost);
		}

		public virtual Stock[] ToStocks()
		{
			return Lines.Select(x => new Stock {
				Product = x.Product,
				ProductId = x.ProductId,
				Producer = x.Producer,
				ProducerId = x.ProductId,
				Count = x.Quantity,
				Cost = x.Cost,
				RetailCost = x.RetailCost,
			}).ToArray();
		}
	}
}
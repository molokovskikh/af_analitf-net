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
		public virtual uint Id { get; set; }
		public virtual DateTime Date { get; set; }
		public virtual Supplier Supplier { get; set; }
		public virtual DateTime? DueDate { get; set; }
		public virtual DateTime? CloseDate { get; set; }
		public virtual Address Address { get; set; }
		public virtual ReceiveStatus Status { get; set; }

		public virtual ulong? OrderId { get; set; }
		public virtual DateTime? OrderDate { get; set; }

		public virtual uint? WaybillId { get; set; }
		public virtual DateTime? WaybillDate { get; set; }

		public virtual decimal Sum { get; set; }
		public virtual decimal RetailSum { get; set; }
		public virtual int LineCount { get; set; }

		public virtual string StatusName => DescriptionHelper.GetDescription(Status);

		public static void Stock(ISession session, User user, Settings settings, SentOrder[] orders)
		{
			foreach (var order in orders) {
				var receiving = new ReceivingOrder {
					Date = DateTime.Now,
					Supplier = session.Load<Supplier>(order.Price.SupplierId),
					OrderDate = order.SentOn,
					OrderId = order.ServerId,
					DueDate = DateTime.Now.AddDays(1),
					Address = order.Address,
				};

				order.Lines.Each(x => x.CalculateRetailCost(settings.Markups, Enumerable.Empty<uint>().ToList(), user, order.Address));
				var lines = order.Lines.Select(x => new ReceivingLine {
						Product = x.ProductSynonym,
						Producer = x.ProducerSynonym,
						Count = x.Count,
						Cost = x.Cost,
						RetailCost = x.RetailCost.GetValueOrDefault(),
					})
					.ToArray();
				foreach (var line in lines)
					line.UpdateStatus();
				receiving.UpdateStat(lines);
				session.Save(receiving);
				order.ReceivingOrderId = receiving.Id;
				lines.Each(x => x.ReceivingOrderId = receiving.Id);
				session.SaveEach(lines);
			}
		}

		public virtual void UpdateStat(IEnumerable<ReceivingLine> lines)
		{
			LineCount = lines.Count();
			Sum = lines.Sum(x => x.Sum);
			RetailSum = lines.Sum(x => x.RetailCost);
			if (lines.Any(x => x.Status == ReceivingLineStatus.New))
				Status = ReceiveStatus.New;
			else if (lines.Any(x => x.Status == ReceivingLineStatus.Closed))
				Status = ReceiveStatus.Closed;
			else
				Status = ReceiveStatus.InProgress;
		}

		public virtual List<Stock> Receive(IList<ReceivingLine> lines)
		{
			var stocks = lines.SelectMany(x => x.Details)
				.Where(x => x.Status == DetailStatus.New)
				.Select(x => x.ToStock())
				.ToList();

			foreach (var line in lines)
				line.UpdateStatus();
			UpdateStat(lines);
			return stocks;
		}
	}
}
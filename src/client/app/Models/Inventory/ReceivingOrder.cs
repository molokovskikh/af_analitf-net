using System;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Helpers;
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
					Supplier = session.Load<Supplier>(order.Price.SupplierId),
					OrderDate = order.SentOn,
					OrderId = order.ServerId,
					DueDate = DateTime.Now.AddDays(1),
					Status = ReceiveStatus.InProgress,
					Address = order.Address,
				};

				order.Lines.Each(x => x.CalculateRetailCost(settings.Markups, Enumerable.Empty<uint>().ToList(), user, order.Address));
				var lines = order.Lines.Select(x => new Stock {
						Product = x.ProductSynonym,
						Producer = x.ProducerSynonym,
						Count = x.Count,
						Cost = x.Cost,
						RetailCost = x.RetailCost.GetValueOrDefault(),
						Status = StockStatus.InTransit
					})
					.ToArray();
				receiving.LineCount = lines.Length;
				receiving.Sum = lines.Sum(x => x.Sum);
				receiving.RetailSum = lines.Sum(x => x.RetailCost);
				session.Save(receiving);
				order.ReceivingOrderId = receiving.Id;
				lines.Each(x => x.ReceivingOrderId = receiving.Id);
				session.SaveEach(lines);
			}
		}
	}
}
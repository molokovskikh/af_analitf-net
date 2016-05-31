using System;
using System.ComponentModel;
using System.Linq;
using Common.NHibernate;
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
		public virtual DateTime OrderDate { get; set; }
		public virtual DateTime? DueDate { get; set; }
		public virtual DateTime? CloseDate { get; set; }
		public virtual decimal Sum { get; set; }
		public virtual ReceiveStatus Status { get; set; }
		public virtual uint OrderId { get; set; }

		public static void Stock(ISession session, SentOrder[] orders)
		{
			foreach (var order in orders) {
				var receiving = new ReceivingOrder {
					Supplier = session.Load<Supplier>(order.Price.SupplierId),
					OrderDate = order.SentOn,
					DueDate = DateTime.Now.AddDays(1),
					Status = ReceiveStatus.InProgress,
					Sum = order.Sum
				};
				session.Save(receiving);
				order.ReceivingOrderId = receiving.Id;
				session.SaveEach(order.Lines.Select(x => new Stock {
					Product = x.ProductSynonym,
					Producer = x.ProducerSynonym,
					Count = x.Count,
					ReceivingOrderId = receiving.Id
				}));
			}
		}
	}
}
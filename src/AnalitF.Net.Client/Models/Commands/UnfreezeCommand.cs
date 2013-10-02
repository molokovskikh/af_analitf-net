using System;
using System.Linq;
using System.Text;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Models.Commands
{
	public class UnfreezeCommand<T> : DbCommand where T : class, IOrder
	{
		private uint[] ids;
		private uint addressId;

		public UnfreezeCommand(uint id)
		{
			ids = new[] { id };
		}

		public UnfreezeCommand(uint[] ids, Address address = null)
		{
			this.ids = ids;
			if (address != null)
				addressId = address.Id;
		}

		public override void Execute()
		{
			var log = new StringBuilder();
			foreach (var id in ids) {
				var order = Session.Load<T>(id);
				try {
					NHibernateUtil.Initialize(order.Address);
				}
				catch(ObjectNotFoundException) {
					var currentOrder = order as Order;
					if (currentOrder != null)
						currentOrder.Address = null;
				}
				try {
					NHibernateUtil.Initialize(order.Price);
				}
				catch(ObjectNotFoundException) {
					var currentOrder = order as Order;
					if (currentOrder != null)
						currentOrder.Price = null;
				}
				var address = Session.Get<Address>(addressId);
				Unfreeze(order, address, Session, log);
			}
			Result = log.ToString();
		}

		public static Order Unfreeze(IOrder sourceOrder, Address addressToOverride, ISession session, StringBuilder log)
		{
			var address = addressToOverride ?? sourceOrder.Address;
			if (address == null) {
				log.AppendLine(String.Format("Заказ №{0} невозможно восстановить, т.к. адрес доставки больше не доступен.", sourceOrder.Id));
				return null;
			}

			if (sourceOrder.Price == null) {
				log.AppendLine(String.Format("Заказ №{0} невозможно восстановить, т.к. прайс-листа нет в обзоре.", sourceOrder.Id));
				return null;
			}

			var destOrder = session.Query<Order>().FirstOrDefault(o => o.Id != sourceOrder.Id
				&& o.Address == address
				&& o.Price == sourceOrder.Price
				&& !o.Frozen);

			if (destOrder == null) {
				destOrder = new Order(sourceOrder.Price, address) {
					Comment = sourceOrder.Comment,
					PersonalComment = sourceOrder.PersonalComment
				};
			}

			foreach (var line in sourceOrder.Lines.ToArray()) {
				var offers = session.Query<Offer>().Where(o => o.ProductSynonymId == line.ProductSynonymId
					&& o.ProducerSynonymId == line.ProducerSynonymId
					&& o.Price == sourceOrder.Price
					&& o.Code == line.Code
					&& o.RequestRatio == line.RequestRatio
					&& o.MinOrderCount == line.MinOrderCount
					&& o.MinOrderSum == line.MinOrderSum)
					.OrderBy(o => o.Cost)
					.ToArray();
				Merge(destOrder, sourceOrder, line, offers, log);
			}

			var currentOrder = sourceOrder as Order;
			if (currentOrder != null && currentOrder.IsEmpty)
				session.Delete(sourceOrder);

			if (!destOrder.IsEmpty)
				session.Save(destOrder);

			return destOrder;
		}

		public static void Merge(Order order, IOrder sourceOrder, IOrderLine orderline, Offer[] offers, StringBuilder log)
		{
			var rest = orderline.Count;
			foreach (var offer in offers) {
				if (rest == 0)
					break;

				var line = order.Lines.FirstOrDefault(l => l.OfferId == offer.Id);
				if (line == null) {
					line = new OrderLine(order, offer, rest);
					line.Count = line.CalculateAvailableQuantity(line.Count);
					if (line.Count > 0)
						order.AddLine(line);
					rest = rest - line.Count;
				}
				else {
					var toOrder = line.Count + rest;
					line.Count = line.CalculateAvailableQuantity(toOrder);
					rest = toOrder - line.Count;
				}
			}

			if (sourceOrder is Order) {
				if (rest == 0)
					((Order)sourceOrder).RemoveLine((OrderLine)orderline);
				else
					((OrderLine)orderline).Count = rest;
			}

			if (rest > 0) {
				if (rest == orderline.Count) {
					log.AppendLine(String.Format("{0} : {1} - {2} ; Предложений не найдено",
						order.Price.Name,
						orderline.ProductSynonym,
						orderline.ProducerSynonym));
				}
				else {
					log.AppendLine(String.Format("{0} : {1} - {2} ; Уменьшено заказное количество {3} вместо {4}",
						sourceOrder.Price.Name,
						orderline.ProductSynonym,
						orderline.ProducerSynonym,
						orderline.Count - rest,
						orderline.Count));
				}
			}
		}
	}
}
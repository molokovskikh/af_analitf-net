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

		public bool Restore;

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
				var address = Session.Get<Address>(addressId);
				Unfreeze(order, address, Session, log);
			}
			Result = log.ToString();
		}

		public Order Unfreeze(IOrder sourceOrder, Address addressToOverride, ISession session, StringBuilder log)
		{
			bool addressNotFound;
			try {
				addressNotFound = sourceOrder.Address == null || sourceOrder.Address.Name == "";
			}
			catch(ObjectNotFoundException) {
				addressNotFound = true;
			}

			if (addressNotFound) {
				if (ShouldCalculateStatus(sourceOrder)) {
					var order = ((Order)sourceOrder);
					order.SendResult = OrderResultStatus.Reject;
					order.SendError = "Адрес доставки больше не доступен";
				}
				log.AppendLine(String.Format("Заказ №{0} невозможно восстановить, т.к. адрес доставки больше не доступен.", sourceOrder.Id));
				return null;
			}

			bool priceNotFound;
			try {
				priceNotFound = sourceOrder.Price == null || sourceOrder.Price.Name == "";
			}
			catch(ObjectNotFoundException) {
				priceNotFound = true;
			}

			if (priceNotFound) {
				if (ShouldCalculateStatus(sourceOrder)) {
					var order = ((Order)sourceOrder);
					order.SendResult = OrderResultStatus.Reject;
					order.SendError = "Прайс-листа нет в обзоре";
				}
				log.AppendLine(String.Format("Заказ №{0} невозможно восстановить, т.к. прайс-листа нет в обзоре.", sourceOrder.Id));
				return null;
			}
			var address = addressToOverride ?? sourceOrder.Address;

			var destOrder = session.Query<Order>().FirstOrDefault(o => o.Id != sourceOrder.Id
				&& o.Address == address
				&& o.Price == sourceOrder.Price
				&& !o.Frozen);

			if (destOrder == null) {
				destOrder = new Order(sourceOrder.Price, address) {
					Comment = sourceOrder.Comment,
					PersonalComment = sourceOrder.PersonalComment
				};
				if (Restore) {
					destOrder.CreatedOn = sourceOrder.CreatedOn;
				}
			}

			foreach (var line in sourceOrder.Lines.ToArray()) {
				var offers = session.Query<Offer>().Where(o => o.ProductSynonymId == line.ProductSynonymId
					&& o.ProducerSynonymId == line.ProducerSynonymId
					&& o.Price == sourceOrder.Price
					&& o.Code == line.Code
					&& o.RequestRatio == line.RequestRatio
					&& o.MinOrderCount == line.MinOrderCount
					&& o.MinOrderSum == line.MinOrderSum
					&& o.BuyingMatrixType != BuyingMatrixStatus.Denied)
					.ToArray()
					.OrderBy(o => o.ResultCost)
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

		public void Merge(Order order, IOrder sourceOrder, IOrderLine sourceLine, Offer[] offers, StringBuilder log)
		{
			var rest = sourceLine.Count;
			foreach (var offer in offers) {
				if (rest == 0)
					break;

				uint ordered;
				var line = order.TryOrder(offer, rest, out ordered);
				if (line != null) {
					if (ShouldCalculateStatus(line)) {

						if (sourceLine.Count == ordered)
							line.ExportId = ((OrderLine)sourceLine).ExportId;

						if (line.Cost != sourceLine.Cost) {
							line.SendResult |= LineResultStatus.CostChanged;
							line.NewCost = line.Cost;
							line.OldCost = sourceLine.Cost;
							line.HumanizeSendError();
						}
						if (line.Count != sourceLine.Count) {
							line.SendResult |= LineResultStatus.QuantityChanged;
							line.NewQuantity = line.Count;
							line.OldQuantity = sourceLine.Count;
							line.HumanizeSendError();
						}
					}
					rest = rest - ordered;
				}
			}

			if (sourceOrder is Order) {
				var srcLine = ((OrderLine)sourceLine);
				if (rest == 0) {
					((Order)sourceOrder).RemoveLine(srcLine);
				}
				else {
					srcLine.Count = rest;
				}
			}

			if (rest > 0) {
				if (rest == sourceLine.Count) {
					if (ShouldCalculateStatus(sourceLine)) {
						((OrderLine)sourceLine).SendResult = LineResultStatus.NoOffers;
						((OrderLine)sourceLine).HumanizeSendError();
					}
					log.AppendLine(String.Format("{0} : {1} - {2} ; Предложений не найдено",
						order.Price.Name,
						sourceLine.ProductSynonym,
						sourceLine.ProducerSynonym));
				}
				else {
					if (ShouldCalculateStatus(sourceLine)) {
						((OrderLine)sourceLine).SendResult = LineResultStatus.CountReduced;
						((OrderLine)sourceLine).HumanizeSendError();
					}
					log.AppendLine(String.Format("{0} : {1} - {2} ; Уменьшено заказное количество {3} вместо {4}",
						sourceOrder.Price.Name,
						sourceLine.ProductSynonym,
						sourceLine.ProducerSynonym,
						sourceLine.Count - rest,
						sourceLine.Count));
				}
			}
		}

		private bool ShouldCalculateStatus(IOrder sourceOrder)
		{
			return sourceOrder is Order && Restore;
		}

		private bool ShouldCalculateStatus(IOrderLine orderline)
		{
			return orderline is OrderLine && Restore;
		}
	}
}
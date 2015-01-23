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

		public void Unfreeze(IOrder sourceOrder, Address addressToOverride, ISession session, StringBuilder log)
		{
			if (!sourceOrder.IsAddressExists()) {
				if (ShouldCalculateStatus(sourceOrder)) {
					var order = ((Order)sourceOrder);
					order.SendResult = OrderResultStatus.Reject;
					order.SendError = "Адрес доставки больше не доступен";
				}
				log.AppendLine(String.Format("Заказ №{0} невозможно {1}, т.к. адрес доставки больше не доступен.", sourceOrder.DisplayId, GuesAction(sourceOrder)));
				return;
			}

			if (!sourceOrder.IsPriceExists()) {
				if (ShouldCalculateStatus(sourceOrder)) {
					var order = ((Order)sourceOrder);
					order.SendResult = OrderResultStatus.Reject;
					order.SendError = "Прайс-листа нет в обзоре";
				}
				log.AppendLine(String.Format("Заказ №{0} невозможно {1}, т.к. прайс-листа нет в обзоре.", sourceOrder.DisplayId, GuesAction(sourceOrder)));
				return;
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

			var count = session.Query<Offer>().Count(o => o.Price == sourceOrder.Price);
			if (count == 0) {
				log.AppendLine(String.Format("Заказ №{0} невозможно {3}, т.к. прайс-листа {1} - {2} нет в обзоре",
					sourceOrder.DisplayId,
					sourceOrder.Price.Name,
					sourceOrder.Price.RegionName,
					GuesAction(sourceOrder)));
				return;
			}

			foreach (var line in sourceOrder.Lines.ToArray()) {
				var offers = Offer.Orderable(session.Query<Offer>())
					.Where(o => o.ProductSynonymId == line.ProductSynonymId
						&& o.ProducerSynonymId == line.ProducerSynonymId
						&& o.Price == sourceOrder.Price
						&& o.Code == line.Code
						&& o.RequestRatio == line.RequestRatio
						&& o.MinOrderCount == line.MinOrderCount
						&& o.MinOrderSum == line.MinOrderSum)
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
		}

		private string GuesAction(IOrder sourceOrder)
		{
			if (Restore)
				return "восстановить";
			if (sourceOrder is Order && ((Order)sourceOrder).Frozen)
				return "\"разморозить\"";
			if (sourceOrder is SentOrder)
				return "вернуть в работу";
			return "объединить";
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

						if (sourceLine.Count == ordered) {
							line.ExportId = ((OrderLine)sourceLine).ExportId;
							line.ExportBatchLineId = ((OrderLine)sourceLine).ExportBatchLineId;
						}

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
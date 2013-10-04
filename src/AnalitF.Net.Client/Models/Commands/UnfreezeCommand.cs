﻿using System;
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

		public bool CalculateStatus;

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

		public void Merge(Order order, IOrder sourceOrder, IOrderLine orderline, Offer[] offers, StringBuilder log)
		{
			var rest = orderline.Count;
			foreach (var offer in offers) {
				if (rest == 0)
					break;

				var existLine = order.Lines.FirstOrDefault(l => l.OfferId == offer.Id);
				if (existLine == null) {
					var line = new OrderLine(order, offer, rest);
					line.Count = line.CalculateAvailableQuantity(line.Count);
					if (ShouldCalculateStatus(line)) {
						if (line.Cost != orderline.Cost) {
							line.SendResult |= LineResultStatus.CostChanged;
							line.NewCost = line.Cost;
							line.OldCost = orderline.Cost;
							line.HumanizeSendError();
						}
						if (line.Count != orderline.Count) {
							line.SendResult |= LineResultStatus.QuantityChanged;
							line.NewQuantity = line.Count;
							line.OldQuantity = orderline.Count;
							line.HumanizeSendError();
						}
					}
					if (line.Count > 0)
						order.AddLine(line);
					rest = rest - line.Count;
				}
				else {
					var toOrder = existLine.Count + rest;
					existLine.Count = existLine.CalculateAvailableQuantity(toOrder);
					rest = toOrder - existLine.Count;
				}
			}

			if (sourceOrder is Order) {
				var srcLine = ((OrderLine)orderline);
				if (rest == 0) {
					((Order)sourceOrder).RemoveLine(srcLine);
				}
				else {
					srcLine.Count = rest;
				}
			}

			if (rest > 0) {
				if (rest == orderline.Count) {
					if (ShouldCalculateStatus(orderline)) {
						((OrderLine)orderline).SendResult = LineResultStatus.NoOffers;
						((OrderLine)orderline).HumanizeSendError();
					}
					log.AppendLine(String.Format("{0} : {1} - {2} ; Предложений не найдено",
						order.Price.Name,
						orderline.ProductSynonym,
						orderline.ProducerSynonym));
				}
				else {
					if (ShouldCalculateStatus(orderline)) {
						((OrderLine)orderline).SendResult = LineResultStatus.CountReduced;
						((OrderLine)orderline).HumanizeSendError();
					}
					log.AppendLine(String.Format("{0} : {1} - {2} ; Уменьшено заказное количество {3} вместо {4}",
						sourceOrder.Price.Name,
						orderline.ProductSynonym,
						orderline.ProducerSynonym,
						orderline.Count - rest,
						orderline.Count));
				}
			}
		}

		private bool ShouldCalculateStatus(IOrder sourceOrder)
		{
			return sourceOrder is Order && CalculateStatus;
		}

		private bool ShouldCalculateStatus(IOrderLine orderline)
		{
			return orderline is OrderLine && CalculateStatus;
		}
	}
}
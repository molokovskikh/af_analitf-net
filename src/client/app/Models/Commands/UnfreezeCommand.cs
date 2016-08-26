using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping;

namespace AnalitF.Net.Client.Models.Commands
{
	public class UnfreezeCommand<T> : DbCommand where T : class, IOrder
	{
		private uint[] ids;
		private uint addressId;

		public bool Restore;

		public UnfreezeCommand()
		{
			//что бы в логах было имя покороче
			Log = LogManager.GetLogger(typeof(UnfreezeCommand<>));
		}

		public UnfreezeCommand(uint id)
			: this()
		{
			ids = new[] { id };
		}

		/// <summary>
		/// action - название операции если передается будет производиться протоколирование
		/// </summary>
		public UnfreezeCommand(uint[] ids, Address address = null)
			: this()
		{
			this.ids = ids;
			if (address != null)
				addressId = address.Id;
		}

		public override void Execute()
		{
			var log = new StringBuilder();
			if (ids.Length == 0)
				return;

			var orders = new List<Order>();
			foreach (var id in ids) {
				var order = Session.Load<T>(id);
				Log.Info($"Попытка {GuesAction(order)} заказ {order:full}");
				var address = Session.Get<Address>(addressId);
				var resultOrder = Unfreeze(order, address, Session, log);
				if (resultOrder != null) {
					orders.Add(resultOrder);
					Session.Save(resultOrder);
				}
				if (ShouldCalculateStatus(order)) {
					var currentOrder = order as Order;
					if (!currentOrder.IsEmpty) {
						orders.Add(currentOrder);
					}
				}
				if (resultOrder != null)
					Log.Info($"Операция завершена успешно, сформирован заказ {resultOrder:full}");
				else
					Log.Info("Не удалось сформировать заказ");
			}
			if (Restore)
				Result = OrderLine.RestoreReport(orders);
			else
				Result = log.ToString();
		}

		public Order Unfreeze(IOrder sourceOrder, Address addressToOverride, ISession session, StringBuilder log)
		{
			var action = GuesAction(sourceOrder);
			if (!sourceOrder.IsAddressExists()) {
				if (ShouldCalculateStatus(sourceOrder)) {
					var order = (Order)sourceOrder;
					order.SendResult = OrderResultStatus.Reject;
					order.SendError = Restore
						? "Заказ был заморожен, т.к. адрес доставки больше не доступен"
						: "Адрес доставки больше не доступен";
				}
				log.AppendLine($"Заказ №{sourceOrder.DisplayId} невозможно" +
					$" {action}, т.к. адрес доставки больше не доступен.");
				return null;
			}

			if (!sourceOrder.IsPriceExists()) {
				if (ShouldCalculateStatus(sourceOrder)) {
					var order = (Order)sourceOrder;
					order.SendResult = OrderResultStatus.Reject;
					order.SendError = Restore ? "Заказ был заморожен, т.к. прайс-листа нет в обзоре" : "Прайс-листа нет в обзоре";
				}
				log.AppendLine($"Заказ №{sourceOrder.DisplayId} невозможно" +
						$" {action}, т.к. прайс-листа нет в обзоре.");
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

			var anyOffer = session.Query<Offer>().Any(o => o.Price == sourceOrder.Price);
			if (!anyOffer) {
				if (ShouldCalculateStatus(sourceOrder)) {
					var order = (Order)sourceOrder;
					order.SendResult = OrderResultStatus.Reject;
					order.SendError = Restore ? "Заказ был заморожен, т.к. прайс-листа нет в обзоре" : "Прайс-листа нет в обзоре";
				}
				log.AppendLine(String.Format("Заказ №{0} невозможно {3}, т.к. прайс-листа {1} - {2} нет в обзоре",
					sourceOrder.DisplayId,
					sourceOrder.Price.Name,
					sourceOrder.Price.RegionName,
					action));
				return null;
			}

			var ids = sourceOrder.Lines.Select(l => l.ProductSynonymId).ToArray();
			var orderOffers = new Offer[0];
			if (ids.Length > 0)
				orderOffers = Offer.Orderable(StatelessSession.Query<Offer>())
					.Where(o => ids.Contains(o.ProductSynonymId) && o.Price == sourceOrder.Price)
					.Fetch(o => o.Price)
					.ToArray();
			foreach (var line in sourceOrder.Lines.ToArray()) {
				var offers = orderOffers
					.Where(o => o.ProductSynonymId == line.ProductSynonymId
						&& o.ProducerSynonymId == line.ProducerSynonymId
						&& o.Price.Id == sourceOrder.Price.Id
						&& o.Code == line.Code
						&& o.RequestRatio == line.RequestRatio
						&& o.MinOrderCount == line.MinOrderCount
						&& o.MinOrderSum == line.MinOrderSum
						&& o.Junk == line.Junk)
					.ToArray()
					.OrderBy(o => o.ResultCost)
					.ToArray();
				Merge(destOrder, sourceOrder, line, offers, log);
			}

			var currentOrder = sourceOrder as Order;
			if (currentOrder != null && currentOrder.IsEmpty)
				session.Delete(sourceOrder);

			if (destOrder.IsEmpty)
				return null;
			if (action == "вернуть в работу")
				destOrder.KeepId = null;
			return destOrder;
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
			var action = GuesAction(sourceOrder);
			var rest = sourceLine.Count;
			foreach (var offer in offers) {
				if (rest == 0)
					break;

				uint ordered;
				var oldDisplayId = sourceOrder.DisplayId;
				if (oldDisplayId == 0) {
					oldDisplayId = sourceOrder.Id;
				}
				var line = order.TryOrder(offer, rest, out ordered);
				if (line != null) {
					if (action == "восстановить" || action == "\"разморозить\"")
						line.Order.KeepId = oldDisplayId;
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
					}
					rest = rest - ordered;
				}
			}

			if (sourceOrder is Order) {
				var srcLine = (OrderLine)sourceLine;
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
					log.AppendLine(
						$"{order.Price.Name} : {sourceLine.ProductSynonym} - {sourceLine.ProducerSynonym} ; Предложений не найдено");
				} else {
					if (ShouldCalculateStatus(sourceLine)) {
						((OrderLine)sourceLine).SendResult = LineResultStatus.CountReduced;
						((OrderLine)sourceLine).HumanizeSendError();
					}
					log.AppendLine(
						$"{sourceOrder.Price.Name} : {sourceLine.ProductSynonym} - {sourceLine.ProducerSynonym}" +
							$" ; Уменьшено заказное количество {sourceLine.Count - rest} вместо {sourceLine.Count}");
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
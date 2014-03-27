using System;
using System.Linq;
using System.Text;
using NHibernate.Linq;
using Environment = System.Environment;

namespace AnalitF.Net.Client.Models.Commands
{
	public class ReorderCommand<T> : DbCommand where T : class, IOrder
	{
		private uint id;

		public ReorderCommand(uint id)
		{
			this.id = id;
		}

		public override void Execute()
		{
			var order = Session.Load<T>(id);
			var orders = Session.Query<Order>().Where(o => o.Address == order.Address
				&& !o.Frozen)
				.ToArray()
				.Where(o => o != order)
				.ToArray();
			var prices = orders.Select(o => o.Price).Where(p => p != order.Price).ToArray();
			var priceIds = prices.Select(o => o.Id.PriceId).ToArray();
			var regionIds = prices.Select(o => o.Id.RegionId).ToArray();
			var productIds = order.Lines.Select(l => l.ProductId).ToArray();
			var offers = Session.Query<Offer>()
				.Where(o => priceIds.Contains(o.Price.Id.PriceId)
					&& regionIds.Contains(o.Price.Id.RegionId)
					&& productIds.Contains(o.ProductId)
					&& o.BuyingMatrixType != BuyingMatrixStatus.Denied)
				.ToArray();
			var log = new StringBuilder();
			Reorder(order, orders, offers, log);

			var currentOrder = order as Order;
			if (currentOrder != null && currentOrder.IsEmpty)
				Session.Delete(currentOrder);

			if (log.Length > 0) {
				log.Insert(0, "   " + String.Format("прайс-лист {0}", order.Price == null ? "<отсутствует>" : order.Price.Name) + Environment.NewLine);
			}
			Result = log.ToString();
		}

		public static void Reorder(IOrder order, Order[] orders, Offer[] offers, StringBuilder log)
		{
			var currentOrder = order as Order;
			foreach (var srcLine in order.Lines.ToArray()) {
				var toOrder = offers.Where(o => o.ProductId == srcLine.ProductId && o.ProducerId == srcLine.ProducerId)
					.OrderBy(o => o.ResultCost)
					.ToArray();

				if (toOrder.Length == 0)
					CalculateLog(log, srcLine);

				foreach (var offer in toOrder) {
					var destOrder = orders.First(o => o.Price == offer.Price);
					if (destOrder == null)
						continue;

					var existLine = destOrder.Lines.FirstOrDefault(l => l.OfferId == offer.Id);
					if (existLine == null) {
						var destLine = new OrderLine(destOrder, offer, srcLine.Count);
						if (destLine.IsCountValid()) {
							CalculateLog(log, srcLine, destLine);
							destOrder.AddLine(destLine);

							if (currentOrder != null)
								currentOrder.RemoveLine((OrderLine)srcLine);

							break;
						}
					}
					else {
						var requiredCount = existLine.Count + srcLine.Count;
						if (existLine.CalculateAvailableQuantity(requiredCount) == requiredCount) {
							CalculateLog(log, srcLine, existLine);
							existLine.Count = requiredCount;

							if (currentOrder != null)
								currentOrder.RemoveLine((OrderLine)srcLine);
							break;
						}
					}
				}
			}
		}

		public static void CalculateLog(StringBuilder log, IOrderLine srcLine, OrderLine destLine = null)
		{
			var prefix = "      ";
			var reason = "";
			if (destLine == null) {
				reason = "предложение отсутствует";
			}
			else if (srcLine.Cost != destLine.Cost && srcLine.Count != destLine.Count) {
				reason = "имеются различия с прайс-листом в цене и количестве заказанного препарата";
			}
			else if (srcLine.Count != destLine.Count && destLine.Count < srcLine.Count) {
				reason = "доступное количество препарата в прайс-листе меньше заказанного ранее";
			}
			else if (srcLine.Count != destLine.Count) {
				reason = "позиция была объединена";
			}
			else if (srcLine.Cost != destLine.Cost) {
				reason = "имеется различие в цене препарата";
			}

			if (String.IsNullOrEmpty(reason))
				return;

			log.Append(prefix);
			if (destLine == null) {
				log.AppendLine(String.Format("{0} - {1} : {2} (старая цена: {3}; старый заказ: {4})",
					srcLine.ProductSynonym,
					srcLine.ProducerSynonym,
					reason,
					srcLine.Cost,
					srcLine.Count));
			}
			else {
				log.AppendLine(String.Format("{0} - {1} : {2} (старая цена: {3}; старый заказ: {4}; новая цена: {5}; новый заказ: {6})",
					srcLine.ProductSynonym,
					srcLine.ProducerSynonym,
					reason,
					srcLine.Cost,
					srcLine.Count,
					destLine.Cost,
					destLine.Count));
			}
		}
	}
}
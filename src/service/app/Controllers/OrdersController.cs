using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using AnalitF.Net.Service.Models;
using Common.Models;
using Common.Models.Helpers;
using Common.Models.Repositories;
using Common.MySql;
using Common.NHibernate;
using Common.Tools;
using log4net.Util;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Service.Controllers
{
	public class OrdersController : JobController2
	{
		public HttpResponseMessage Post(SyncRequest request)
		{
			return StartJob((s, c, l) => {
				var result = SaveOrders(s, l, request.Orders, request.Force);
				File.WriteAllText(l.OutputFile(c), JsonConvert.SerializeObject(result));
			});
		}

		public HttpResponseMessage Put(ConfirmRequest confirm)
		{
			Session.CreateSQLQuery(@"
update Orders.OrdersHead o
join Logs.AcceptedOrderLogs l on l.OrderId = o.RowId
set o.Deleted = 0
where l.RequestId = :id;

delete l from Logs.AcceptedOrderLogs l
where l.RequestId = :id;")
				.SetParameter("id", confirm.RequestId)
				.ExecuteUpdate();
			var job = Session.Get<RequestLog>(confirm.RequestId);
			if (job != null)
				job.Confirm(Config);
			return new HttpResponseMessage(HttpStatusCode.OK);
		}

		private static List<OrderResult> SaveOrders(ISession session, RequestLog log, ClientOrder[] clientOrders, bool force)
		{
			var user = log.User;
			var errors = new List<OrderResult>();
			if (clientOrders == null)
				return errors;

			using(StorageProcedures.GetActivePrices((MySqlConnection)session.Connection, user.Id)) {
				var orderitemMap = new Dictionary<OrderItem, uint>();
				var orders = new List<Order>();
				var rules = session.Load<OrderRules>(user.Client.Id);
				foreach (var clientOrder in clientOrders) {
					var address = session.Load<Address>(clientOrder.AddressId);
					var price = session.Load<PriceList>(clientOrder.PriceId);

					var activePrice = session.Get<ActivePrice>(new PriceKey(price, clientOrder.RegionId));
					//мы должны принимать заказы даже если прайс-лист больше не активен, например устарел
					//как мне кажется смысла в этом не но так было всегда
					if (activePrice == null) {
						activePrice = new ActivePrice {
							Id = new PriceKey(price, clientOrder.RegionId),
							PriceDate = clientOrder.PriceDate
						};
					}

					try {
						var order = new Order(activePrice, user, address, rules) {
							ClientOrderId = clientOrder.ClientOrderId,
							PriceDate = clientOrder.PriceDate,
							ClientAddition = clientOrder.Comment,
							CalculateLeader = false
						};
						foreach (var sourceItem in clientOrder.Items) {
							var offer = new Offer {
								Id = new OfferKey(sourceItem.OfferId.OfferId, sourceItem.OfferId.RegionId),
								Cost = (float)sourceItem.Cost,
								PriceList = activePrice,
								PriceCode = price.PriceCode,
								CodeFirmCr = sourceItem.ProducerId,
							};

							var properties = typeof(BaseOffer).GetProperties().Where(p => p.CanRead && p.CanWrite);
							foreach (var property in properties) {
								var value = property.GetValue(sourceItem, null);
								property.SetValue(offer, value, null);
							}

							var item = order.AddOrderItem(offer, sourceItem.Count);
							item.CostWithDelayOfPayment = ((float?)sourceItem.ResultCost).GetValueOrDefault(item.CostWithDelayOfPayment);
							if (sourceItem.MinCost != null) {
								item.LeaderInfo = new OrderItemLeadersInfo {
									OrderItem = item,
									MinCost = (float?)sourceItem.MinCost,
									PriceCode = sourceItem.MinPrice != null
										? (uint?)sourceItem.MinPrice.PriceId : null,
									LeaderMinCost = (float?)sourceItem.LeaderCost,
									LeaderPriceCode = sourceItem.LeaderPrice != null
										? (uint?)sourceItem.LeaderPrice.PriceId : null,
								};
							}
							orderitemMap.Add(item, sourceItem.Id);
						}
						if (order.OrderItems.Count > 0) {
							orders.Add(order);
						}
					}
					catch(OrderException e) {
						Log.Warn(String.Format("Не удалось принять заказ {0}", clientOrder.ClientOrderId), e);
						errors.Add(new OrderResult(clientOrder.ClientOrderId, "Отправка заказов запрещена"));
					}
				}

				errors.AddEach(Validate(session, user, orders, force, orderitemMap));

				//мы не должны сохранять валидные заказы если корректировка заказов
				//включена
				if (!user.UseAdjustmentOrders || errors.Count == 0) {
					orders.Each(o => o.Deleted = true);
					session.SaveEach(orders);
					session.SaveEach(orders.Select(o => new AcceptedOrderLog(log, o)));
				}

				return errors.Concat(orders.Select(o => new OrderResult(o, orderitemMap))).ToList();
			}
		}

		private static List<OrderResult> Validate(ISession session, User user,
			List<Order> orders, bool force, Dictionary<OrderItem, uint> orderitemMap)
		{
			var errors = new List<OrderResult>();
			var addressId = orders.Select(o => o.AddressId).FirstOrDefault();
			if (addressId == null)
				return errors;
			try {
				var cheker = new PreorderChecker((MySqlConnection)session.Connection, user.Client);
				cheker.Check();
				PreorderChecker.CheckDailyOrdersSum(session, session.Load<Address>(addressId), orders);
			}
			catch(OrderException e) {
				errors.AddRange(orders.Select(o => new OrderResult(o.ClientOrderId, e.Message)));
				orders.Clear();
			}

			if (!user.IgnoreCheckMinOrder) {
				foreach (var order in orders.ToArray()) {
					var controller = new MinReqController(session, order);
					var result = controller.ProcessOrder();
					if (result != null) {
						var message = result.Type == MinReqStatus.ErrorType.MinReq
							? String.Format("Поставщик отказал в приеме заказа." +
								" Сумма заказа меньше минимально допустимой." +
								" Минимальный заказ {0:C} заказано {1:C}.",
								result.MinReq, order.CalculateSum())
							: String.Format("Поставщик отказал в приеме дозаказа." +
								" Сумма дозаказа меньше минимально допустимой." +
								" Минимальный дозаказ {0:C} заказано {1:C}.",
								result.MinReordering, order.CalculateSum());
						errors.Add(new OrderResult(order.ClientOrderId, message));
						orders.Remove(order);
					}
				}
			}

			var checker = new GroupSumOrderChecker(session);
			var rejectedOrders = checker.Check(orders);
			orders.RemoveEach(rejectedOrders.Keys);
			errors = errors.Concat(rejectedOrders.Keys
				.Select(o => new OrderResult(o.ClientOrderId, String.Format("Сумма заказов в этом" +
					" месяце по Вашему предприятию превысила установленный лимит." +
					" Лимит заказа на поставщика {0} - {1:C}", o.PriceList.Supplier.Name, rejectedOrders[o]))))
				.ToList();

			if (user.UseAdjustmentOrders && !force) {
				var optimizer = new CostOptimizer((MySqlConnection)session.Connection,
					user.Client.Id,
					user.Id);

				var ids = orders.SelectMany(o => o.OrderItems).Select(i => i.ProductId).ToArray();
				if (ids.Length > 0) {
					var query = new OfferQuery();
					query.Where("c0.ProductId in (:ProductIds)");
					var offers = session.CreateSQLQuery(query.ToSql())
						.AddEntity("Offer", typeof(Offer))
						.SetParameterList("ProductIds", ids)
						.List<Offer>();
					var activePrices = session.Query<ActivePrice>().ToList();
					offers.Each(o => o.PriceList = activePrices.First(
						price => price.Id.Price.PriceCode == o.PriceCode && price.Id.RegionCode == o.Id.RegionCode));

					foreach (var order in orders.ToArray()) {
						var results = new List<OrderLineResult>();
						foreach (var item in order.OrderItems) {
							var offer = FindOffer(offers, item);
							var result = Check(offer, item,
								order.ActivePrice.Id.Price.Supplier.Id == optimizer.SupplierId,
								orderitemMap);
							results.Add(result);
						}

						if (results.Any(r => r.Result != LineResultStatus.OK)) {
							orders.Remove(order);
							errors.Add(new OrderResult(order.ClientOrderId,
								"В заказе обнаружены позиции с измененной ценой или количеством",
								results));
						}
					}
				}
			}

			return errors;
		}

		private static Offer FindOffer(IEnumerable<Offer> offers, OrderItem item)
		{
			var comparers = new Func<Offer, OrderItem, bool>[] {
				(o, i) => o.PriceList.Id == item.Order.ActivePrice.Id,
				(o, i) => o.ProductId == i.ProductId,
				(o, i) => o.SynonymCode == i.SynonymCode,
				(o, i) => o.SynonymFirmCrCode == i.SynonymFirmCrCode,
				(o, i) => o.SynonymFirmCrCode == i.SynonymFirmCrCode,
				(o, i) => o.Code == i.Code,
				(o, i) => o.CodeCr == i.CodeCr,
				(o, i) => o.Junk == i.Junk,
				(o, i) => o.RequestRatio == i.RequestRatio,
				(o, i) => o.OrderCost == i.OrderCost,
				(o, i) => o.MinOrderCount == i.MinOrderCount,
			};
			return offers.FirstOrDefault(o => o.Id.CoreId == item.CoreId)
				?? offers.FirstOrDefault(o => comparers.All(c => c(o, item)));
		}

		private static OrderLineResult Check(Offer offer, OrderItem item, bool ignoreCostReduce,
			Dictionary<OrderItem, uint> orderitemMap)
		{
			var result = new OrderLineResult(orderitemMap[item]);

			if (offer == null) {
				result.Result = LineResultStatus.NoOffers;
				return result;
			}

			result.ServerCost = (decimal?)offer.Cost;
			result.ServerQuantity = offer.Quantity;

			if (offer.Quantity != null && offer.Quantity < item.Quantity) {
				result.Result |= LineResultStatus.QuantityChanged;
			}

			if (item.Cost < offer.Cost
				|| (!ignoreCostReduce && item.Cost > offer.Cost)) {
				result.Result |= LineResultStatus.CostChanged;
			}

			return result;
		}
	}
}
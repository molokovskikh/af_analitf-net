using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web;
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
			//теоретически здесь лимит может стать меньше нуля, игнорируем эту возможность как незначительную
			Session.CreateSQLQuery(@"
update Orders.OrdersHead o
join Logs.AcceptedOrderLogs l on l.OrderId = o.RowId
set o.Deleted = 0
where l.RequestId = :id;

delete l from Logs.AcceptedOrderLogs l
where l.RequestId = :id;

update OrderSendRules.SmartOrderLimits l
join Logs.PendingLimitLogs p on p.LimitId = l.Id
set l.Value = l.Value - p.Value, l.ToDay = l.ToDay - p.ToDay
where p.RequestId = :id;

delete l from Logs.PendingLimitLogs l
where l.RequestId = :id;")
				.SetParameter("id", confirm.RequestId)
				.ExecuteUpdate();
			var job = Session.Get<RequestLog>(confirm.RequestId);
			job?.Confirm(Config, confirm.Message);
			return new HttpResponseMessage(HttpStatusCode.OK);
		}

		private static List<OrderResult> SaveOrders(ISession session, RequestLog log, ClientOrder[] clientOrders, bool force)
		{
			var user = log.User;
			var rules = session.Load<OrderRules>(user.Client.Id);
			var errors = new List<OrderResult>();
			if (clientOrders == null)
				return errors;

			using(StorageProcedures.GetActivePrices((MySqlConnection)session.Connection, user.Id)) {
				var orderitemMap = new Dictionary<OrderItem, uint>();
				var orders = new List<Order>();
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
							CalculateLeader = false,
							PriceName = clientOrder.PriceName,
							CostId = clientOrder.CostId,
							CostName = clientOrder.CostName,
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
							//клиент в поле уценка передает уценку с учетом клиентских настроек
							//оригинальная уценка хранится в поле OriginalJunk
							offer.Junk = sourceItem.OriginalJunk;

							try {
								var item = order.AddOrderItem(offer, sourceItem.Count);
								item.CostWithDelayOfPayment = ((float?)sourceItem.ResultCost).GetValueOrDefault(item.CostWithDelayOfPayment);
								if (sourceItem.MinCost != null) {
									item.LeaderInfo = new OrderItemLeadersInfo {
										OrderItem = item,
										MinCost = (float?)sourceItem.MinCost,
										PriceCode = sourceItem.MinPrice?.PriceId,
										LeaderMinCost = (float?)sourceItem.LeaderCost,
										LeaderPriceCode = sourceItem.LeaderPrice?.PriceId,
									};
								}
								orderitemMap.Add(item, sourceItem.Id);
							}
							catch(OrderException e) {
								//если здесь произошла ошибка значит есть проблема в клиентском приложении и нужно узнать об этом
								Log.Error(String.Format("Не удалось сформировать заявку по позиции {0}", offer.Id), e);
								throw new OrderException(String.Format("Не удалось сформировать заявку по позиции {0}", offer.Id), e);
							}
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
				//удаление дублей должно производиться после всех проверок тк заказ будет отброшен полностью
				//и проверки для него не важны или пройдет часть позиций которые пользователь "донабил"
				//в этом случае проверки для них не должны применяться тк они идут в "догонку"
				errors.AddEach(RemoveDuplicate(session, orders, orderitemMap));

				//мы не должны сохранять валидные заказы если корректировка заказов
				//включена
				if (!user.UseAdjustmentOrders || errors.Count == 0) {
					var addressIds = orders.Select(o => o.AddressId).Distinct().ToArray();
					if (addressIds.Length > 0) {
						var addresses = session.Query<Address>()
							.Where(a => addressIds.Contains(a.Id) && a.OrderLimits.Count > 0)
							.ToArray();
						foreach (var order in orders) {
							var limit = addresses.Where(a => a.Id == order.AddressId)
								.SelectMany(a => a.OrderLimits)
								.FirstOrDefault(l => l.Supplier.Id == order.PriceList.Supplier.Id);
							if (limit != null) {
								session.Save(new PendingLimitLog(log, limit, limit.ConsumeLimit((decimal)order.CalculateSum())));
							}
						}
					}
					//отмечаем заказа как удаленный до подтверждения
					orders.Each(o => o.Deleted = true);
					session.SaveEach(orders);
					session.SaveEach(orders.Select(o => new AcceptedOrderLog(log, o)));
				}

				errors = errors.Concat(orders.Select(o => new OrderResult(o, orderitemMap))).ToList();
			}
			//информацию по отказам сохраняем для техподдержки
			foreach (var result in errors.Where(x => x.Result == OrderResultStatus.Reject)) {
				if (!String.IsNullOrEmpty(log.Error)) {
					log.Error += Environment.NewLine;
				}
				var order = clientOrders.FirstOrDefault(x => x.ClientOrderId == result.ClientOrderId);
				if (order == null)
					continue;
				var price = session.Get<PriceList>(order.PriceId);
				log.Error += String.Format("Заказ {0} на сумму {1} на поставщика {2} был отклонен по причине: {3}",
					result.ClientOrderId,
					order.Items.Sum(x => x.Count * x.Cost),
					price != null ? price.Supplier.Name : "",
					result.Error);
			}

			//в результате удаления дублей для одного клиентского заказа может быть сформировано два результата
			//один на приемку части заявки второй на удаленные позиций-дублей
			//нужно объединить эти данные
			return errors.GroupBy(x => Tuple.Create(x.ClientOrderId, x.Result))
				.Select(x => {
					if (x.Count() == 1)
						return x.First();
					var dst = x.First();
					dst.ServerOrderId = x.Max(y => y.ServerOrderId);
					dst.Lines.AddRange(x.Skip(1).SelectMany(y => y.Lines));
					return dst;
				})
				.ToList();
		}

		public static OrderResult[] RemoveDuplicate(ISession session, List<Order> orders,
			Dictionary<OrderItem, uint> lineMap)
		{
			var results = new List<OrderResult>();
			foreach (var order in orders) {
				var begin = order.WriteTime.AddMinutes(-60);
				var lines = session.Query<OrderItem>()
					.Where(x => x.Order.AddressId == order.AddressId
						&& x.Order.UserId == order.UserId
						&& x.Order.PriceList == order.PriceList
						&& x.Order.ClientOrderId == order.ClientOrderId
						&& x.Order.RegionCode == order.RegionCode
						&& x.Order.Deleted == false
						&& x.Order.WriteTime > begin)
					.ToArray();
				var result = new OrderResult {
					ClientOrderId = order.ClientOrderId.GetValueOrDefault(),
					Result = OrderResultStatus.OK,
				};
				foreach (var line in order.OrderItems.ToArray()) {
					var duplicate = FirstDuplicate(lines, line);
					if (duplicate == null)
						continue;
					Log.WarnFormat("Строка {3} заявки {0} на {1} в количестве {2} отброшена как дубль",
						order.PriceList.Supplier.Name,
						order.ClientOrderId,
						line.Quantity,
						line.CoreId);
					order.RemoveItem(line);
					result.ServerOrderId = duplicate.Order.RowId;
					result.Lines.Add(new OrderLineResult(lineMap[line], duplicate.RowId));
				}
				if (result.Lines.Count > 0)
					results.Add(result);
			}
			var duplicates = orders.Where(x => x.OrderItems.Count == 0).ToArray();
			orders.RemoveEach(duplicates);

			foreach (var order in duplicates) {
				Log.WarnFormat("Заявка {1} на {0} отброшена как дубль.",
					order.PriceList.Supplier.Name,
					order.ClientOrderId);
			}
			return results.ToArray();
		}

		private static OrderItem FirstDuplicate(OrderItem[] existLines, OrderItem line)
		{
			return existLines.FirstOrDefault(x => x.ProductId == line.ProductId
				&& x.SynonymCode == line.SynonymCode
				&& x.SynonymFirmCrCode == line.SynonymFirmCrCode
				&& x.Code == line.Code
				&& x.CodeCr == line.CodeCr
				&& x.Junk == line.Junk
				&& x.Quantity == line.Quantity);
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
								" Минимальный заказ {0:0.00} заказано {1:0.00}.",
								result.MinReq, order.CalculateSum())
							: String.Format("Поставщик отказал в приеме дозаказа." +
								" Сумма дозаказа меньше минимально допустимой." +
								" Минимальный дозаказ {0:0.00} заказано {1:0.00}.",
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
					" Лимит заказа на поставщика {0} - {1:0.00}", o.PriceList.Supplier.Name, rejectedOrders[o]))))
				.ToList();


			var rejectedByLimit = Address.CheckLimits(session, orders);
			orders.RemoveEach(rejectedByLimit.Select(r => r.Item1));
			errors = errors.Concat(rejectedByLimit
				.Select(r => new OrderResult(r.Item1.ClientOrderId, String.Format("Сумма заказов " +
					"по Вашему предприятию превысила установленный лимит." +
					" Лимит заказа на поставщика {0} - {1:0.00}", r.Item1.PriceList.Supplier.Name, r.Item2))))
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
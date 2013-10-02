using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Web.Http;
using AnalitF.Net.Service.Helpers;
using AnalitF.Net.Service.Models;
using Common.Models;
using Common.Models.Helpers;
using Common.Models.Repositories;
using Common.MySql;
using Common.NHibernate;
using Common.Tools;
using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Linq;
using Newtonsoft.Json;
using log4net;

namespace AnalitF.Net.Service.Controllers
{
	public class MainController : ApiController
	{
		public static ILog log = LogManager.GetLogger(typeof(MainController));

		public ISession Session { get; set; }
		public User CurrentUser { get; set; }
		public Config.Config Config;

		public HttpResponseMessage Get(bool reset = false, string updateType = null)
		{
			var existsJob = Session.Query<RequestLog>()
				.OrderByDescending(j => j.CreatedOn)
				.FirstOrDefault(j => j.User == CurrentUser);

			if (existsJob != null) {
				if (existsJob.IsStale)
					existsJob = null;
				else if (reset)
					existsJob = null;
			}

			if (existsJob == null) {
				existsJob  = new RequestLog(CurrentUser, RequestHelper.GetVersion(Request));
				existsJob.UpdateType = updateType;
				Session.Save(existsJob);
				Session.Transaction.Commit();

				StartJob(existsJob.Id, Config, Session.SessionFactory);
			}

			if (!existsJob.IsCompleted)
				return new HttpResponseMessage(HttpStatusCode.Accepted);

			return new HttpResponseMessage(HttpStatusCode.OK) {
				Content = new StreamContent(existsJob.GetResult(Config.ResultPath))
			};
		}

		public HttpResponseMessage Delete()
		{
			Session.CreateSQLQuery(@"
update Logs.DocumentSendLogs l
	join Logs.PendingDocLogs p on p.SendLogId = l.Id
set l.Committed = 1
where p.UserId = :userId;

delete from Logs.PendingDocLogs
where UserId = :userId;")
				.SetParameter("userId", CurrentUser.Id)
				.ExecuteUpdate();

			return new HttpResponseMessage();
		}

		public static Task StartJob(uint jobId, Config.Config config, ISessionFactory sessionFactory)
		{
			var task = new Task(() => {
				try {
					using (var logSession = sessionFactory.OpenSession())
					using (var logTransaction = logSession.BeginTransaction()) {
						var job = logSession.Load<RequestLog>(jobId);
						try {
							using(var exportSession = sessionFactory.OpenSession())
							using(var exportTransaction = exportSession.BeginTransaction()) {
								var exporter = new Exporter(exportSession, job.User.Id, job.Version, job.UpdateType) {
									Prefix = job.Id.ToString(),
									ExportPath = config.ExportPath,
									ResultPath = config.ResultPath,
									UpdatePath = config.UpdatePath,
									AdsPath = config.AdsPath,
									DocsPath = config.DocsPath,
									MaxProducerCostPriceId = config.MaxProducerCostPriceId,
									MaxProducerCostCostId = config.MaxProducerCostCostId,
								};
								using (exporter) {
									exporter.ExportCompressed(job.OutputFile);
								}
								exportTransaction.Commit();
							}
						}
						catch(Exception e) {
							log.Error(String.Format("Произошла ошибка при обработке запроса {0}", jobId), e);
							job.Faulted(e);
						}
						finally {
							job.Completed();
							logSession.Save(job);
							logSession.Flush();
							logTransaction.Commit();
						}
					}
				}
				catch(Exception e) {
					log.Error(String.Format("Произошла ошибка при обработке запроса {0}", jobId), e);
				}
			});
			task.Start();
			return task;
		}

		public HttpResponseMessage Post(SyncRequest request)
		{
			var content = new ObjectContent<List<OrderResult>>(SaveOrders(request.Orders, request.Force),
				new JsonMediaTypeFormatter());

			SavePriceSettings(request.Prices);
			return new HttpResponseMessage(HttpStatusCode.OK) {
				Content = content
			};
		}

		private List<OrderResult> SaveOrders(ClientOrder[] clientOrders, bool force)
		{
			var errors = new List<OrderResult>();
			if (clientOrders == null)
				return errors;

			using(StorageProcedures.GetActivePrices((MySqlConnection)Session.Connection, CurrentUser.Id)) {
				var orderitemMap = new Dictionary<OrderItem, uint>();
				var orders = new List<Order>();
				var rules = Session.Load<OrderRules>(CurrentUser.Client.Id);
				foreach (var clientOrder in clientOrders) {
					var address = Session.Load<Address>(clientOrder.AddressId);
					var price = Session.Load<PriceList>(clientOrder.PriceId);

					var activePrice = Session.Get<ActivePrice>(new PriceKey(price, clientOrder.RegionId));
					//мы должны принимать заказы даже если прайс-лист больше не активен, например устарел
					//как мне кажется смысла в этом не но так было всегда
					if (activePrice == null) {
						activePrice = new ActivePrice {
							Id = new PriceKey(price, clientOrder.RegionId),
							PriceDate = clientOrder.PriceDate
						};
					}

					try {
						var order = new Order(activePrice, CurrentUser, address, rules) {
							ClientOrderId = clientOrder.ClientOrderId,
							PriceDate = clientOrder.PriceDate,
							ClientAddition = clientOrder.Comment,
						};
						foreach (var item in clientOrder.Items) {
							var offer = new Offer {
								Id = new OfferKey(item.OfferId.OfferId, item.OfferId.RegionId),
								Cost = (float)item.Cost,
								PriceList = activePrice,
								PriceCode = price.PriceCode,
								CodeFirmCr = item.ProducerId,
							};

							var properties = typeof(BaseOffer).GetProperties().Where(p => p.CanRead && p.CanWrite);
							foreach (var property in properties) {
								var value = property.GetValue(item, null);
								property.SetValue(offer, value, null);
							}

							orderitemMap.Add(order.AddOrderItem(offer, item.Count), item.Id);
						}
						if (order.OrderItems.Count > 0) {
							orders.Add(order);
						}
					}
					catch(OrderException e) {
						log.Warn(String.Format("Не удалось принять заказ {0}", clientOrder.ClientOrderId), e);
						errors.Add(new OrderResult(clientOrder.ClientOrderId, "Отправка заказов запрещена"));
					}
				}

				errors.AddEach(Validate(orders, force, orderitemMap));

				//мы не должны сохранять валидные заказы если корректировка заказов
				//включена
				if (!CurrentUser.UseAdjustmentOrders || errors.Count == 0)
					Session.SaveEach(orders);

				return errors.Concat(orders.Select(o => new OrderResult(o))).ToList();
			}
		}

		private List<OrderResult> Validate(List<Order> orders, bool force, Dictionary<OrderItem, uint> orderitemMap)
		{
			var errors = new List<OrderResult>();
			var addressId = orders.Select(o => o.AddressId).FirstOrDefault();
			if (addressId == null)
				return errors;
			try {
				var address = Session.Load<Address>(addressId.Value);
				var cheker = new PreorderChecker((MySqlConnection)Session.Connection,
					CurrentUser.Client,
					address);
				cheker.Check();
			}
			catch(OrderException e) {
				errors.AddRange(orders.Select(o => new OrderResult(o.ClientOrderId, e.Message)));
				orders.Clear();
			}

			if (!CurrentUser.IgnoreCheckMinOrder) {
				foreach (var order in orders.ToArray()) {
					var context = new MinOrderContext((MySqlConnection)Session.Connection, Session, order);
					var controller = new MinReqController(context);
					var result = controller.ProcessOrder(order);
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

			var checker = new GroupSumOrderChecker(Session);
			var rejectedOrders = checker.Check(orders);
			orders.RemoveEach(rejectedOrders.Keys);
			errors = errors.Concat(rejectedOrders.Keys
				.Select(o => new OrderResult(o.ClientOrderId, String.Format("Сумма заказов в этом" +
					" месяце по Вашему предприятию превысила установленный лимит." +
					" Лимит заказа на поставщика {0} - {1:C}", o.PriceList.Supplier.Name, rejectedOrders[o]))))
				.ToList();

			if (CurrentUser.UseAdjustmentOrders && !force) {
				var optimizer = new CostOptimizer((MySqlConnection)Session.Connection,
					CurrentUser.Client.Id,
					CurrentUser.Id);

				var ids = orders.SelectMany(o => o.OrderItems).Select(i => i.ProductId).ToArray();
				var query = new OfferQuery();
				query.Where("c0.ProductId in (:ProductIds)");
				var offers = Session.CreateSQLQuery(query.ToSql())
					.AddEntity("Offer", typeof(Offer))
					.SetParameterList("ProductIds", ids)
					.List<Offer>();
				var activePrices = Session.Query<ActivePrice>().ToList();
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

			return errors;
		}

		private Offer FindOffer(IEnumerable<Offer> offers, OrderItem item)
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

		private OrderLineResult Check(Offer offer, OrderItem item, bool ignoreCostReduce,
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

		private void SavePriceSettings(PriceSettings[] settings)
		{
			if (settings == null)
				return;

			foreach (var setting in settings) {
				var userPrice = Session.Query<UserPrice>().FirstOrDefault(u => u.User == CurrentUser
					&& u.Price.PriceCode == setting.PriceId
					&& u.RegionId == setting.RegionId);

				if (!setting.Active && userPrice != null) {
					Session.Delete(userPrice);
				}
				else if (setting.Active && userPrice == null) {
					var price = Session.Get<PriceList>(setting.PriceId);
					if (price == null)
						return;
					Session.Save(new UserPrice(CurrentUser, setting.RegionId, price));
				}
			}
		}
	}
}

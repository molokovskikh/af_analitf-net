using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using AnalitF.Net.Service.Controllers;
using AnalitF.Net.Service.Models;
using Common.Models;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Tools.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using Newtonsoft.Json;
using Test.Support;
using Test.Support.Suppliers;

namespace AnalitF.Net.Service.Test
{
	[TestFixture]
	public class ControllerFixture : IntegrationFixture2
	{
		private MainController controller;
		private User user;
		private Config.Config config;
		private TestClient client;
		private FileCleaner tmpFiles;

		[SetUp]
		public void Setup()
		{
			tmpFiles = new FileCleaner();
			config = FixtureSetup.Config;
			client = TestClient.CreateNaked(session);
			session.Save(client);

			user = session.Load<User>(client.Users[0].Id);
			controller = new MainController {
				Request = new HttpRequestMessage(),
				Session = session,
				CurrentUser = user,
				Config = config,
			};
		}

		[TearDown]
		public void TearDown()
		{
			tmpFiles?.Dispose();
		}

		[Test]
		public void Export_data()
		{
			var response = controller.Get();
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
		}

		[Test]
		public void Build_new_update_on_reset()
		{
			Assert.AreEqual(HttpStatusCode.Accepted, controller.Get(true).StatusCode);
			var log = session.Query<RequestLog>().First(r => r.User == user);
			log.Completed();
			log.Confirm(config);
			session.Flush();

			Assert.AreEqual(HttpStatusCode.Accepted, controller.Get(true).StatusCode);
			var requests = session.Query<RequestLog>().Where(r => r.User == user).ToList();
			Assert.That(requests.Count, Is.EqualTo(2));
		}

		[Test]
		public void Ignore_request_while_job_in_progress()
		{
			var message = controller.Get(true);
			Assert.AreEqual(HttpStatusCode.Accepted, message.StatusCode);
			var id = GetRequestId(message);

			message = controller.Get(true);
			Assert.AreEqual(HttpStatusCode.Accepted, message.StatusCode);
			var newId = GetRequestId(message);
			Assert.AreEqual(newId, id);
			var requests = session.Query<RequestLog>().Where(r => r.User == user).ToList();
			Assert.That(requests.Count, Is.EqualTo(1));
		}

		[Test]
		public void Do_not_load_stale_data()
		{
			var job = GetJob();
			job.CreatedOn = DateTime.Now.AddMonths(-1);
			session.Save(job);

			var response = controller.Get();
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
		}

		[Test]
		public void Do_not_build_new_data_if_not_stale()
		{
			config.UpdateLifeTime = TimeSpan.FromMinutes(30);
			var job = GetJob();
			job.Completed();
			session.Save(job);
			FileHelper.Touch(tmpFiles.Watch(job.OutputFile(config)));

			var response = controller.Get(reset: true);
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"user id = {user.Id}");
			Assert.AreEqual(job.Id, Convert.ToUInt32(response.Headers.GetValues("Request-Id").Implode()));
		}

		[Test]
		public void Reset_error()
		{
			var job = GetJob();
			job.Faulted(new Exception("Fail"));
			session.Save(job);
			var response = controller.Get(reset: true);
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
		}

		[Test]
		public void Process_request()
		{
			var job = new RequestLog(user, new Version());
			var task = job.StartJob(session, (_, ___) => {});
			task.Wait();

			session.Refresh(job);
			Assert.That(job.Error, Is.Null);
			Assert.That(job.IsCompleted, Is.True);
			Assert.That(job.IsFaulted, Is.False);
		}

		[Test]
		public void Log_broken_job()
		{
			var job = new RequestLog(user, new Version());
			var task = job.StartJob(session,
				(jobSession, requestJob) => {
					throw new Exception("Тестовое исключение");
				});
			task.Wait();
			session.Refresh(job);
			Assert.That(job.Error, Is.Not.Null);
			Assert.That(job.IsCompleted, Is.True);
			Assert.That(job.IsFaulted, Is.True);
		}

		[Test]
		public void Save_price_settings()
		{
			var supplier = TestSupplier.CreateNaked(session);
			session.Save(supplier);
			supplier.Maintain(session);

			var price = supplier.Prices[0];

			var userPrices = session.Query<UserPrice>().Where(p => p.User == user).ToList();
			var disabledPrice = userPrices.FirstOrDefault(p => p.RegionId == 1 && p.Price.PriceCode == price.Id);
			Assert.That(disabledPrice, Is.Not.Null);

			controller.Post(new SyncRequest {
				Prices = new[] {
					new PriceSettings(price.Id, 1, false)
				}
			});

			userPrices = session.Query<UserPrice>().Where(p => p.User == user).ToList();
			disabledPrice = userPrices.FirstOrDefault(p => p.RegionId == 1 && p.Price.PriceCode == price.Id);
			Assert.That(disabledPrice, Is.Null);
		}

		[Test]
		public void Send_order_for_disabled_price()
		{
			var supplier = TestSupplier.CreateNaked(session);
			var price = supplier.Prices[0];
			supplier.CreateSampleCore(session);
			price.Costs[0].PriceItem.PriceDate = DateTime.Now.AddDays(-10);
			session.Save(supplier);
			supplier.Maintain(session);

			var offer = price.Core[0];
			PostOrder(ToClientOrder(offer));

			var orders = session.Query<Order>()
				.Where(o => o.UserId == user.Id && o.PriceList.PriceCode == price.Id)
				.ToList();

			Assert.That(orders.Count, Is.EqualTo(1));
			Assert.IsFalse(orders[0].CalculateLeader);
		}

		[Test]
		public void Lower_limit_by_actual_sum()
		{
			var supplier = TestSupplier.CreateNaked(session);
			var price = supplier.Prices[0];
			supplier.CreateSampleCore(session);
			price.Costs[0].PriceItem.PriceDate = DateTime.Now.AddDays(-10);
			session.Save(supplier);
			supplier.Maintain(session);

			var address = session.Load<Address>(user.AvaliableAddresses[0].Id);
			var limit = new OrderLimit(session.Load<Supplier>(supplier.Id), 1000) {
				Today = 500
			};
			address.OrderLimits.Add(limit);

			var settings = session.Load<ClientSettings>(user.Client.Id);
			settings.AllowDelayOfPayment = true;

			var offer = price.Core[0];
			var clientOrder = ToClientOrder(offer);
			clientOrder.Orders[0].Items[0].ResultCost = 150;
			session.Flush();
			PostOrder(clientOrder);

			session.Refresh(limit);
			Assert.AreEqual(900, limit.Value);
			Assert.AreEqual(500, limit.Today);
		}

		[Test]
		public void Reject_order()
		{
			var ordersController = new OrdersController {
				Request = new HttpRequestMessage(),
				Session = session,
				CurrentUser = user,
				Config = config,
			};

			var supplier = TestSupplier.CreateNaked(session);
			var price = supplier.Prices[0];
			supplier.CreateSampleCore(session);
			price.Costs[0].PriceItem.PriceDate = DateTime.Now.AddDays(-10);
			session.Save(supplier);
			supplier.Maintain(session);

			var testUser = session.Load<TestUser>(user.Id);
			var intersection = session
				.Query<TestIntersection>().First(i => i.Price == price && i.Client == testUser.Client);
			var addressIntersection = intersection.AddressIntersections[0];
			addressIntersection.ControlMinReq = true;
			addressIntersection.MinReq = 3000;

			user.UseAdjustmentOrders = true;
			session.Flush();
			var offer = price.Core.First();
			var postResult = ordersController.Post(ToClientOrder(offer));
			Assert.That(GetRequestId(postResult), Is.GreaterThan(0));

			ordersController.Task.Wait();
			ordersController.Session.Clear();

			var response = ordersController.Get();
			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			var result = JsonConvert.DeserializeObject<List<OrderResult>>(response.Content.ReadAsStringAsync().Result);
			Assert.AreEqual(OrderResultStatus.Reject, result[0].Result);
			Assert.AreEqual("Поставщик отказал в приеме заказа. Сумма заказа меньше минимально допустимой. Минимальный заказ 3000,00 заказано 100,00.",
				result[0].Error);
		}

		[Test]
		public void Error_on_job_fault()
		{
			controller.Config = new Config.Config {
				InjectedFault = "Тестовое исключение"
			};
			var response = controller.Get(true);
			Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
			var log = session.Query<RequestLog>().First(r => r.User == user);
			WaitHelper.WaitOrFail(30.Second(), () => {
				session.Refresh(log);
				return log.IsFaulted;
			}, "Сломаный лог");
			Assert.IsTrue(log.IsCompleted);
			Assert.IsTrue(log.IsFaulted);
		}

		[Test]
		public void Drop_duplicate_order()
		{
			var supplier = TestSupplier.CreateNaked(session);
			var price = supplier.Prices[0];
			supplier.CreateSampleCore(session);
			price.Costs[0].PriceItem.PriceDate = DateTime.Now.AddDays(-10);
			session.Save(supplier);
			supplier.Maintain(session);
			//нужно что бы сохранить синонимы
			session.Flush();

			var offer = price.Core[0];
			var request = ToClientOrder(offer);
			var results = PostOrder(request);

			var orders = session.Query<TestOrder>()
				.Where(x => x.User.Id == user.Id).ToArray();
			Assert.AreEqual(results[0].ClientOrderId, request.Orders[0].ClientOrderId);
			Assert.AreEqual(results[0].Result, OrderResultStatus.OK);
			Assert.AreEqual(results[0].ServerOrderId, orders[0].Id);

			results = PostOrder(ToClientOrder(offer));
			Assert.AreEqual(results[0].ClientOrderId, request.Orders[0].ClientOrderId);
			Assert.AreEqual(results[0].Result, OrderResultStatus.OK);
			Assert.AreEqual(results[0].ServerOrderId, orders[0].Id);

			orders = session.Query<TestOrder>()
				.Where(x => x.User.Id == user.Id).ToArray();
			Assert.AreEqual(1, orders.Length);
		}

		[Test]
		public void Partial_order_reject()
		{
			var supplier = TestSupplier.CreateNaked(session);
			var price = supplier.Prices[0];
			supplier.CreateSampleCore(session);
			price.Costs[0].PriceItem.PriceDate = DateTime.Now.AddDays(-10);
			session.Save(supplier);
			supplier.Maintain(session);
			//нужно что бы сохранить синонимы
			session.Flush();

			var order = new TestOrder(client.Users[0], price);
			order.AddItem(price.Core[0], 1);
			order.ClientOrderId = 1;
			session.Save(order);
			session.Flush();

			var request = ToClientOrder(price.Core[0], price.Core[1]);
			var results = PostOrder(request);
			var orderResult = results[0];
			Assert.AreEqual(1, results.Count);
			Assert.AreEqual(orderResult.ClientOrderId, request.Orders[0].ClientOrderId);
			Assert.AreEqual(orderResult.Result, OrderResultStatus.OK);
			//Должны сформировать новый заказ
			Assert.AreNotEqual(orderResult.ServerOrderId, order.Id);
			Assert.AreEqual(2, orderResult.Lines.Count, "В ответе должны присутствовать все строки заявки");

			var orders = session.Query<TestOrder>()
				.Where(x => x.User.Id == user.Id)
				.ToArray();
			Assert.AreEqual(2, orders.Length);
			Assert.AreEqual(1, orders[0].Items.Count, $"userid = {user.Id}");
			Assert.AreEqual(1, orders[1].Items.Count, $"userid = {user.Id}");
		}

		[Test]
		public void Build_update_on_error_production()
		{
			config.UpdateLifeTime = TimeSpan.FromMinutes(30);
			var errorLog = new RequestLog(user, new HttpRequestMessage(HttpMethod.Get, "http://localhost/Main/"), controller.GetType().Name);
			errorLog.Faulted(new ExporterException("Пожалуйста, обратитесь в бухгалтерию АналитФармация.", ErrorType.AccessDenied));
			session.Save(errorLog);

			var message = controller.Get(true);
			Assert.AreEqual(message.StatusCode, HttpStatusCode.Accepted);
			Assert.AreNotEqual(errorLog.Id, GetRequestId(message).ToString());
		}

		[Test]
		public void Build_update_on_error_debug()
		{
			var errorLog = new RequestLog(user, new HttpRequestMessage(HttpMethod.Get, "http://localhost/Main/"), controller.GetType().Name);
			errorLog.Faulted(new ExporterException("Пожалуйста, обратитесь в бухгалтерию АналитФармация.", ErrorType.AccessDenied));
			session.Save(errorLog);

			var message = controller.Get(true);
			Assert.AreEqual(message.StatusCode, HttpStatusCode.Accepted);
			Assert.AreNotEqual(errorLog.Id, GetRequestId(message).ToString());
		}

		private RequestLog GetJob()
		{
			var job = new RequestLog(user, new Version()) {
				UpdateType = controller.GetType().Name
			};
			return job;
		}

		private static uint GetRequestId(HttpResponseMessage message)
		{
			var value = ((ObjectContent)message.Content).Value;
			return (uint)value.GetType().GetProperty("RequestId").GetValue(value);
		}

		private List<OrderResult> PostOrder(SyncRequest syncRequest)
		{
			if (!session.Transaction.IsActive)
				session.Transaction.Begin();

			var ordersController = new OrdersController {
				Request = new HttpRequestMessage(),
				Session = session,
				CurrentUser = user,
				Config = config,
			};
			var value = ((ObjectContent)ordersController.Post(syncRequest).Content).Value;
			var id = (uint)value.GetType().GetProperty("RequestId").GetValue(value, null);
			ordersController.Task.Wait();
			session.Refresh(session.Load<RequestLog>(id));
			var results = new List<OrderResult>();
			using (var message = ordersController.Get())
				results = message.Content.ReadAsAsync<List<OrderResult>>().Result;
			ordersController.Put(new ConfirmRequest(id));
			return results;
		}

		private SyncRequest ToClientOrder(params TestCore[] offers)
		{
			var price = offers[0].Price;
			var index = 1u;
			return new SyncRequest {
				Orders = new[] {
					new ClientOrder {
						ClientOrderId = 1,
						PriceId = price.Id,
						RegionId = price.Supplier.HomeRegion.Id,
						AddressId = user.AvaliableAddresses[0].Id,
						CreatedOn = DateTime.Now,
						PriceDate = DateTime.Now,
						Items = offers.Select(offer =>
							new ClientOrderItem {
								Id = index++,
								OfferId = new OfferComposedId {
									OfferId = offer.Id,
									RegionId = price.Supplier.HomeRegion.Id,
								},
								ProductId = offer.Product.Id,
								ProducerId = offer.Producer.Id,
								SynonymCode = offer.ProductSynonym.Id,
								SynonymFirmCrCode = offer.ProducerSynonym?.Id,
								Code = offer.Code,
								CodeCr = "",
								Count = 1,
								Cost = 100
							}).ToArray(),
					}
				}
			};
		}
	}
}

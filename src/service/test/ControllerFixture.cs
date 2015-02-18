using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AnalitF.Net.Service.Controllers;
using AnalitF.Net.Service.Helpers;
using AnalitF.Net.Service.Models;
using Common.Models;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Tools.Helpers;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using Newtonsoft.Json;
using Test.Support;
using Test.Support.Suppliers;
using Test.Support.log4net;

namespace AnalitF.Net.Service.Test
{
	[TestFixture]
	public class ControllerFixture : IntegrationFixture
	{
		private MainController controller;
		private User user;
		private Config.Config config;

		[SetUp]
		public void Setup()
		{
			config = FixtureSetup.Config;
			var client = TestClient.CreateNaked();
			session.Save(client);

			user = session.Load<User>(client.Users[0].Id);
			controller = new MainController {
				Request = new HttpRequestMessage(),
				Session = session,
				CurrentUser = user,
				Config = config,
			};
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
			controller.Get(true);

			session.Transaction.Begin();
			controller.Get(true);

			var requests = session.Query<RequestLog>().Where(r => r.User == user).ToList();
			Assert.That(requests.Count, Is.EqualTo(2));
		}

		[Test]
		public void Do_not_load_stale_data()
		{
			var job = new RequestLog(user, new Version());
			job.CreatedOn = DateTime.Now.AddMonths(-1);
			session.Save(job);

			var response = controller.Get();
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
		}

		[Test]
		public void Reset_error()
		{
			var job = new RequestLog(user, new Version()) {
				IsCompleted = true,
				IsFaulted = true
			};
			session.Save(job);
			var response = controller.Get(true);
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
		}

		[Test]
		public void Process_request()
		{
			var job = new RequestLog(user, new Version());
			var task = RequestHelper.StartJob(session, job, config, session.SessionFactory,
				(jobSession, jobConfig, requestJob) => {});
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
			var task = RequestHelper.StartJob(session, job, config, session.SessionFactory,
				(jobSession, jobConfig, requestJob) => {
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
			supplier.Maintain();

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
			supplier.CreateSampleCore();
			price.Costs[0].PriceItem.PriceDate = DateTime.Now.AddDays(-10);
			session.Save(supplier);
			supplier.Maintain();

			var offer = price.Core[0];
			PostOrder(ToClientOrder(offer));

			var orders = session.Query<Order>()
				.Where(o => o.UserId == user.Id && o.PriceList.PriceCode == price.Id)
				.ToList();

			Assert.That(orders.Count, Is.EqualTo(1));
			//todo: этой проверки здесь не место
			Assert.IsFalse(orders[0].CalculateLeader);
		}

		[Test]
		public void Lower_limit_by_delay_of_payment_sum()
		{
			var supplier = TestSupplier.CreateNaked(session);
			var price = supplier.Prices[0];
			supplier.CreateSampleCore();
			price.Costs[0].PriceItem.PriceDate = DateTime.Now.AddDays(-10);
			session.Save(supplier);
			supplier.Maintain();

			var address = session.Load<Address>(user.AvaliableAddresses[0].Id);
			var limit = new SmartOrderLimit(session.Load<Supplier>(supplier.Id), 1000);
			address.SmartOrderLimits.Add(limit);

			var settings = session.Load<ClientSettings>(user.Client.Id);
			settings.AllowDelayOfPayment = true;

			var offer = price.Core[0];
			var clientOrder = ToClientOrder(offer);
			clientOrder.Orders[0].Items[0].ResultCost = 150;
			session.Flush();
			PostOrder(clientOrder);

			session.Refresh(limit);
			Assert.AreEqual(850, limit.Value);
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
			supplier.CreateSampleCore();
			price.Costs[0].PriceItem.PriceDate = DateTime.Now.AddDays(-10);
			session.Save(supplier);
			supplier.Maintain();

			var testUser = session.Load<TestUser>(user.Id);
			var intersection = session
				.Query<TestIntersection>().First(i => i.Price == price && i.Client == testUser.Client);
			var addressIntersection = intersection.AddressIntersections[0];
			addressIntersection.ControlMinReq = true;
			addressIntersection.MinReq = 3000;

			user.UseAdjustmentOrders = true;
			session.Flush();
			var offer = price.Core.First();
			var postResult = ((ObjectContent)ordersController.Post(ToClientOrder(offer)).Content).Value;
			Assert.That(postResult.GetType().GetProperty("RequestId").GetValue(postResult), Is.GreaterThan(0));

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

		private void PostOrder(SyncRequest syncRequest)
		{
			var ordersController = new OrdersController {
				Request = new HttpRequestMessage(),
				Session = session,
				CurrentUser = user,
				Config = config,
			};
			ordersController.Post(syncRequest);
			ordersController.Task.Wait();
		}

		private SyncRequest ToClientOrder(TestCore offer)
		{
			var supplier = offer.Price.Supplier;
			return new SyncRequest {
				Orders = new[] {
					new ClientOrder {
						ClientOrderId = 1,
						PriceId = offer.Price.Id,
						RegionId = supplier.HomeRegion.Id,
						AddressId = user.AvaliableAddresses[0].Id,
						CreatedOn = DateTime.Now,
						PriceDate = DateTime.Now,
						Items = new[] {
							new ClientOrderItem {
								OfferId = new OfferComposedId {
									OfferId = offer.Id,
									RegionId = supplier.HomeRegion.Id,
								},
								ProductId = offer.Product.Id,
								ProducerId = offer.Producer.Id,
								Count = 1,
								Cost = 100
							},
						}
					}
				}
			};
		}
	}
}

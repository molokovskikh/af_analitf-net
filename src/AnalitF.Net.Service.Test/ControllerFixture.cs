using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AnalitF.Net.Service.Controllers;
using AnalitF.Net.Service.Models;
using Common.Models;
using Common.Tools;
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

		[SetUp]
		public void Setup()
		{
			var client = TestClient.CreateNaked();
			session.Save(client);

			user = session.Load<User>(client.Users[0].Id);
			controller = new MainController {
				Request = new HttpRequestMessage(),
				Session = session,
				CurrentUser = user
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
			session.Save(job);
			session.Transaction.Commit();

			var task = MainController.StartJob(job.Id, FixtureSetup.Config, session.SessionFactory);
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
			session.Save(job);

			var config = new Config.Config();
			var data = JsonConvert.SerializeObject(FixtureSetup.Config);
			JsonConvert.PopulateObject(data, config);
			config.LocalExportPath = "non-exist-path";
			config.RemoteExportPath = "non-exist-path";

			session.Transaction.Commit();
			var task = MainController.StartJob(job.Id, config, session.SessionFactory);
			task.Wait();
			session.Refresh(job);
			Assert.That(job.Error, Is.Not.Null);
			Assert.That(job.IsCompleted, Is.True);
			Assert.That(job.IsFaulted, Is.True);
		}

		[Test]
		public void Save_price_settings()
		{
			var supplier = TestSupplier.CreateNaked();
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
			var supplier = TestSupplier.CreateNaked();
			var price = supplier.Prices[0];
			supplier.CreateSampleCore();
			price.Costs[0].PriceItem.PriceDate = DateTime.Now.AddDays(-10);
			session.Save(supplier);
			supplier.Maintain();

			var offer = price.Core[0];
			controller.Post(ToClientOrder(offer));

			var orders = session.Query<Order>()
				.Where(o => o.UserId == user.Id && o.PriceList.PriceCode == price.Id)
				.ToList();

			Assert.That(orders.Count, Is.EqualTo(1));
			//todo: этой проверки здесь не место
			Assert.IsFalse(orders[0].CalculateLeader);
		}

		[Test]
		public void Reject_order()
		{
			var supplier = TestSupplier.CreateNaked();
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
			var result = (List<OrderResult>)((ObjectContent)controller.Post(ToClientOrder(offer)).Content).Value;
			Assert.AreEqual(OrderResultStatus.Reject, result[0].Result);
			Assert.AreEqual("Поставщик отказал в приеме заказа. Сумма заказа меньше минимально допустимой. Минимальный заказ 3 000,00р. заказано 100,00р..",
				result[0].Error);
		}

		private SyncRequest ToClientOrder(TestCore offer)
		{
			var supplier = offer.Price.Supplier;
			return new SyncRequest {
				Orders = new [] {
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

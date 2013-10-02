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
		private ISession localSession;
		private User user;

		[SetUp]
		public void Setup()
		{
			var client = TestClient.CreateNaked();
			session.Save(client);
			session.Flush();
			session.Transaction.Commit();

			localSession = FixtureSetup.Factory.OpenSession();
			localSession.BeginTransaction();

			user = localSession.Load<User>(client.Users[0].Id);
			controller = new MainController {
				Request = new HttpRequestMessage(),
				Session = localSession,
				CurrentUser = user
			};
		}

		[TearDown]
		public void TearDown()
		{
			localSession.Dispose();
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

			localSession.Dispose();
			localSession = FixtureSetup.Factory.OpenSession();
			localSession.BeginTransaction();
			controller.Session = localSession;
			controller.Get(true);

			var requests = localSession.Query<RequestLog>().Where(r => r.User == user).ToList();
			Assert.That(requests.Count, Is.EqualTo(2));
		}

		[Test]
		public void Do_not_load_stale_data()
		{
			var job = new RequestLog(user, new Version());
			job.CreatedOn = DateTime.Now.AddMonths(-1);
			localSession.Save(job);

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
			localSession.Save(job);
			var response = controller.Get(true);
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
		}

		[Test]
		public void Process_request()
		{
			var job = new RequestLog(user, new Version());
			localSession.Save(job);
			localSession.Transaction.Commit();

			var task = MainController.StartJob(job.Id, FixtureSetup.Config, localSession.SessionFactory);
			task.Wait();
			localSession.Clear();
			localSession.Refresh(job);
			Assert.That(job.Error, Is.Null);
			Assert.That(job.IsCompleted, Is.True);
			Assert.That(job.IsFaulted, Is.False);
		}

		[Test]
		public void Log_broken_job()
		{
			var job = new RequestLog(user, new Version());
			localSession.Save(job);
			localSession.Transaction.Commit();

			var config = new Config.Config();
			var data = JsonConvert.SerializeObject(FixtureSetup.Config);
			JsonConvert.PopulateObject(data, config);
			config.ExportPath = "non-exist-path";

			var task = MainController.StartJob(job.Id, config, localSession.SessionFactory);
			task.Wait();
			localSession.Refresh(job);
			Assert.That(job.Error, Is.Not.Null);
			Assert.That(job.IsCompleted, Is.True);
			Assert.That(job.IsFaulted, Is.True);
		}

		[Test]
		public void Save_price_settings()
		{
			session.BeginTransaction();
			var supplier = TestSupplier.CreateNaked();
			session.Save(supplier);
			supplier.Maintain();
			session.Transaction.Commit();

			var price = supplier.Prices[0];

			var userPrices = localSession.Query<UserPrice>().Where(p => p.User == user).ToList();
			var disabledPrice = userPrices.FirstOrDefault(p => p.RegionId == 1 && p.Price.PriceCode == price.Id);
			Assert.That(disabledPrice, Is.Not.Null);

			controller.Post(new SyncRequest {
				Prices = new[] {
					new PriceSettings(price.Id, 1, false)
				}
			});

			userPrices = localSession.Query<UserPrice>().Where(p => p.User == user).ToList();
			disabledPrice = userPrices.FirstOrDefault(p => p.RegionId == 1 && p.Price.PriceCode == price.Id);
			Assert.That(disabledPrice, Is.Null);
		}

		[Test]
		public void Send_order_for_disabled_price()
		{
			session.BeginTransaction();
			var supplier = TestSupplier.CreateNaked();
			var price = supplier.Prices[0];
			supplier.CreateSampleCore();
			price.Costs[0].PriceItem.PriceDate = DateTime.Now.AddDays(-10);
			session.Save(supplier);
			supplier.Maintain();
			session.Transaction.Commit();

			var offer = price.Core[0];
			controller.Post(new SyncRequest {
				Orders = new [] {
					new ClientOrder {
						ClientOrderId = 1,
						PriceId = price.Id,
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
			});

			var orders = localSession.Query<Order>()
				.Where(o => o.UserId == user.Id && o.PriceList.PriceCode == price.Id)
				.ToList();

			Assert.That(orders.Count, Is.EqualTo(1));
		}
	}
}

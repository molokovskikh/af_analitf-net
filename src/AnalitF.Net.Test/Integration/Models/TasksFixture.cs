using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Subjects;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using Ionic.Zip;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Test.Support.Documents;
using Test.Support.log4net;
using log4net.Config;

namespace AnalitF.Net.Test.Integration.Models
{
	[TestFixture]
	public class TasksFixture : IntegrationFixture
	{
		private ISession localSession;
		private Task<UpdateResult> task;
		private CancellationTokenSource cancelletion;
		private string updatePath;
		private BehaviorSubject<Progress> progress;
		private CancellationToken token;

		private HttpSelfHostServer server;
		private Uri uri;
		private bool restoreUser;

		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			FileHelper.InitDir("service",
				"service/data/export",
				"service/data/result",
				"service/data/update",
				"service/data/ads");
			uri = new Uri("http://localhost:7018");
			var cfg = new HttpSelfHostConfiguration(uri);
			cfg.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
			cfg.ClientCredentialType = HttpClientCredentialType.Windows;

			Application.Init();
			Application.Configure(cfg);

			server = new HttpSelfHostServer(cfg);
			server.OpenAsync().Wait();
		}

		[SetUp]
		public void Setup()
		{
			restoreUser = false;

			updatePath = @"service/data/update";
			Tasks.BaseUri = uri;
			Tasks.ArchiveFile = Path.Combine("temp", "archive.zip");
			Tasks.ExtractPath = Path.Combine("temp", "update");
			Tasks.RootPath = "app";

			var files = Directory.GetFiles(".", "*.txt");
			foreach (var file in files) {
				File.Delete(file);
			}

			FileHelper.InitDir(updatePath, Tasks.RootPath, "temp");

			localSession = SetupFixture.Factory.OpenSession();
			cancelletion = new CancellationTokenSource();
			token = cancelletion.Token;
			progress = new BehaviorSubject<Progress>(new Progress());

			task = new Task<UpdateResult>(t => Tasks.UpdateTask(null, token, progress), token);
		}

		[TearDown]
		public void Teardown()
		{
			SetupFixture.RestoreData(localSession);
			if (restoreUser) {
				session.Flush();
				var user = localSession.Query<User>().First();
				session.CreateSQLQuery("update Customers.Users set Login = Id;" +
					"update Customers.Users set Login = :login where Id = :id")
					.SetParameter("login", Environment.UserName)
					.SetParameter("id", user.Id)
					.ExecuteUpdate();
			}
		}

		[TestFixtureTearDown]
		public void FixtureTearDown()
		{
			server.CloseAsync().Wait();
			server.Dispose();
		}

		[Test]
		public void Import()
		{
			localSession.CreateSQLQuery("delete from offers").ExecuteUpdate();

			task.Start();
			task.Wait();
			Assert.That(task.Exception, Is.Null);
			var offers = localSession.CreateSQLQuery("select * from offers").List();
			Assert.That(offers.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Import_version_update()
		{
			File.WriteAllBytes(Path.Combine(updatePath, "updater.exe"), new byte[0]);
			File.WriteAllText(Path.Combine(updatePath, "version.txt"), "99.99.99.99");

			task.Start();
			task.Wait();
			Assert.That(task.Result, Is.EqualTo(UpdateResult.UpdatePending));
		}

		[Test]
		public void Sent_price_settings_changes()
		{
			ExportImportFixture.CreateUser(session);
			session.Transaction.Commit();
			restoreUser = true;

			var price = localSession.Query<Price>().First(p => p.PositionCount > 0);
			Assert.That(price.Active, Is.True);
			Assert.That(price.PositionCount, Is.GreaterThan(0));
			price.Active = false;
			localSession.Flush();

			task.Start();
			task.Wait();

			localSession.Refresh(price);
			Assert.That(price.Active, Is.False, price.Id.ToString());
			Assert.That(price.PositionCount, Is.EqualTo(0));
			var offersCount = localSession.Query<Offer>().Count(o => o.Price == price);
			Assert.That(offersCount, Is.EqualTo(0));

			var user = session.Query<TestUser>().First(u => u.Login == Environment.UserName);
			var priceSettings = session
				.CreateSQLQuery("select * from Customers.UserPrices where UserId = :userId and PriceId = :priceId and RegionId = :regionId")
				.SetParameter("userId", user.Id)
				.SetParameter("priceId", price.Id.PriceId)
				.SetParameter("regionId", price.Id.RegionId)
				.List();
			Assert.That(priceSettings.Count, Is.EqualTo(0));
		}

		[Test]
		public void Send_logs()
		{
			var begin = DateTime.Now;
			File.WriteAllText(@"app\AnalitF.Net.Client.log", "123");
			task.Start();
			task.Wait();

			var user = session.Query<TestUser>().First(u => u.Login == Environment.UserName);
			var text = session.CreateSQLQuery("select Text from Logs.ClientAppLogs where UserId = :userId and CreatedOn >= :date")
				.SetParameter("userId", user.Id)
				.SetParameter("date", begin)
				.UniqueResult<string>();
			Assert.That(text, Is.EqualTo("123"));
		}

		[Test]
		public void Send_orders()
		{
			var address = localSession.Query<Address>().First();
			task = new Task<UpdateResult>(t => Tasks.SendOrders(null, token, progress, address), token);

			var begin = DateTime.Now;
			Offer offer;
			using (localSession.BeginTransaction()) {
				localSession.CreateSQLQuery("delete from orders").ExecuteUpdate();
				offer = localSession.Query<Offer>().First();
				var order = new Order(offer.Price, address);
				order.AddLine(offer, 1);
				localSession.Save(order);
			}

			Update();

			Assert.That(localSession.Query<Order>().Count(), Is.EqualTo(0));
			var sentOrders = localSession.Query<SentOrder>().Where(o => o.SentOn >= begin).ToList();
			Assert.That(sentOrders.Count, Is.EqualTo(1));
			Assert.That(sentOrders[0].Lines.Count, Is.EqualTo(1));

			var orders = session.Query<TestOrder>().Where(o => o.WriteTime >= begin).ToList();
			Assert.That(orders.Count, Is.EqualTo(1));
			var resultOrder = orders[0];
			Assert.That(resultOrder.RowCount, Is.EqualTo(1));
			var item = resultOrder.Items[0];
			Assert.That(item.CodeFirmCr, Is.EqualTo(offer.ProducerId));
			Assert.That(item.SynonymCode, Is.EqualTo(offer.ProductSynonymId));
			Assert.That(item.SynonymFirmCrCode, Is.EqualTo(offer.ProducerSynonymId));
		}

		[Test]
		public void Import_waybill()
		{
			var user = session.Query<TestUser>().First(u => u.Login == Environment.UserName);
			var supplier = user.GetActivePricesNaked(session).First().Price.Supplier;
			var log = new TestDocumentLog(supplier, user.AvaliableAddresses[0], "");
			var waybill = new TestWaybill(log);
			waybill.Lines.Add(new TestWaybillLine(waybill) {
				Product = "Азарга капли глазные 5мл Фл.-кап. Х1",
				Certificates = "РОСС BE.ФМ11.Д06711",
				CertificatesDate = "01.16.2013",
				Period = "30.09.2014",
				Producer = "Алкон-Куврер н.в. с.а.",
				Country = "БЕЛЬГИЯ",
				SupplierCostWithoutNDS = 536.17m,
				SupplierCost = 589.79m,
				Quantity = 1,
				SerialNumber = "A 565",
				Amount = 589.79m,
				NDS = 10,
				NDSAmount = 53.62m,
			});
			waybill.Lines.Add(new TestWaybillLine(waybill) {
				Product = "Доксазозин 4мг таб. Х30 (R)",
				Certificates = "РОСС RU.ФМ08.Д38737",
				Period = "01.05.2017",
				Producer = "Нью-Фарм Инк./Вектор-Медика ЗАО, РОССИЯ",
				ProducerCost = 213.18m,
				RegistryCost = 382.89m,
				SupplierPriceMarkup = -5.746m,
				SupplierCostWithoutNDS = 200.93m,
				SupplierCost = 221.03m,
				Quantity = 2,
				VitallyImportant = true,
				NDS = 10,
				SerialNumber = "21012",
				Amount = 442.05m,
				NDSAmount = 40.19m,
				BillOfEntryNumber = "10609010/101209/0004305/1",
				EAN13 = "4605635002748",
			});
			for(var i = 0; i < 10; i++)
				waybill.Lines.Add(new TestWaybillLine(waybill) {
					Product = "Доксазозин 4мг таб. Х30 (R)",
					Certificates = "РОСС RU.ФМ08.Д38737",
					Period = "01.05.2017",
					Producer = "Нью-Фарм Инк./Вектор-Медика ЗАО, РОССИЯ",
				});
			session.Save(waybill);
			var sendLog = new TestDocumentSendLog(user, log);
			session.Save(sendLog);

			Update();

			var waybills = localSession.Query<Waybill>().ToList();
			Assert.That(waybills.Count(), Is.EqualTo(1));
			Assert.That(waybills[0].Sum, Is.GreaterThan(0));
			Assert.That(waybills[0].RetailSum, Is.GreaterThan(0));
		}

		[Test]
		public void Repair_data_base()
		{
			Directory.GetFiles("data", "mnns.*").Each(File.Delete);
			File.WriteAllBytes(Path.Combine("data", "markupconfigs.frm"), new byte[0]);

			var result = Tasks.CheckAndRepairDb(cancelletion.Token);

			Assert.That(result, Is.False);
			Assert.That(Directory.GetFiles("data", "mnns.*").Length, Is.EqualTo(3));
			Assert.That(new FileInfo(Path.Combine("data", "markupconfigs.frm")).Length, Is.GreaterThan(0));
		}

		[Test]
		public void Clean_db()
		{
			Tasks.CleanDb(token);

			Assert.That(localSession.Query<Offer>().Count(), Is.EqualTo(0));
			Assert.That(localSession.Query<Settings>().Count(), Is.EqualTo(1));
		}

		[Test]
		public void Import_after_update()
		{
			File.Copy(Directory.GetFiles(@"service/data/result").Last(), Tasks.ArchiveFile);
			using(var file = new ZipFile(Tasks.ArchiveFile))
				file.ExtractAll(Tasks.ExtractPath);

			localSession.CreateSQLQuery("delete from offers").ExecuteUpdate();
			Tasks.Import(null, token, progress);
			Assert.That(localSession.Query<Offer>().Count(), Is.GreaterThan(0));
		}

		[Test]
		public void Clean_after_import()
		{
			File.WriteAllBytes(Path.Combine(updatePath, "updater.exe"), new byte[0]);
			File.WriteAllText(Path.Combine(updatePath, "version.txt"), "99.99.99.99");

			task.Start();
			task.Wait();
			Assert.That(task.Result, Is.EqualTo(UpdateResult.UpdatePending));

			File.Delete(Path.Combine(updatePath, "updater.exe"));
			File.Delete(Path.Combine(updatePath, "version.txt"));

			task = new Task<UpdateResult>(t => Tasks.UpdateTask(null, token, progress), token);
			task.Start();
			task.Wait();
			Assert.That(task.Result, Is.EqualTo(UpdateResult.OK));
		}

		private void Update()
		{
			session.Flush();
			session.Transaction.Commit();

			task.Start();
			task.Wait();
		}
	}
}
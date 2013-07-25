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
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Service;
using AnalitF.Net.Service.Config;
using Common.Tools;
using Ionic.Zip;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using Newtonsoft.Json;
using Test.Support;
using Test.Support.Documents;
using Test.Support.Suppliers;
using Test.Support.log4net;
using log4net.Config;

namespace AnalitF.Net.Test.Integration.Models
{
	[TestFixture]
	public class UpdateFixture : IntegrationFixture
	{
		private ISession localSession;
		private Task<UpdateResult> task;
		private CancellationTokenSource cancelletion;
		private BehaviorSubject<Progress> progress;
		private CancellationToken token;

		private HttpSelfHostServer server;
		private Uri uri;
		private bool restoreUser;

		private Config serviceConfig;

		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			FileHelper.InitDir("var");
			uri = new Uri("http://localhost:7018");
			var cfg = new HttpSelfHostConfiguration(uri);
			cfg.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
			cfg.ClientCredentialType = HttpClientCredentialType.Windows;

			serviceConfig = Application.InitApp(cfg);

			server = new HttpSelfHostServer(cfg);
			server.OpenAsync().Wait();
		}

		[SetUp]
		public void Setup()
		{
			restoreUser = false;

			Tasks.BaseUri = uri;
			Tasks.ArchiveFile = Path.Combine("temp", "archive.zip");
			Tasks.ExtractPath = Path.Combine("temp", "update");
			Tasks.RootPath = "app";

			var files = Directory.GetFiles(".", "*.txt");
			foreach (var file in files) {
				File.Delete(file);
			}

			FileHelper.InitDir(serviceConfig.UpdatePath, Tasks.RootPath, "temp");

			localSession = SetupFixture.Factory.OpenSession();
			cancelletion = new CancellationTokenSource();
			token = cancelletion.Token;
			progress = new BehaviorSubject<Progress>(new Progress());

			var command = new UpdateCommand(Tasks.ArchiveFile, Tasks.ExtractPath, Tasks.RootPath);
			task = CreateTask(command);
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
			File.WriteAllBytes(Path.Combine(serviceConfig.UpdatePath, "updater.exe"), new byte[0]);
			File.WriteAllText(Path.Combine(serviceConfig.UpdatePath, "version.txt"), "99.99.99.99");

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

			task = CreateTask(new SendOrders(address));

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
			var waybill = DataMother.CreateWaybill(session, user);
			var log = waybill.Log;
			session.Save(waybill);
			var sendLog = new TestDocumentSendLog(user, log);
			session.Save(sendLog);
			var file = waybill.Log.CreateFile(serviceConfig.DocsPath, "waybill content");

			Update();

			var waybills = localSession.Query<Waybill>().ToList();
			Assert.That(waybills.Count(), Is.GreaterThanOrEqualTo(1));
			Assert.That(waybills[0].Sum, Is.GreaterThan(0));
			Assert.That(waybills[0].RetailSum, Is.GreaterThan(0));

			var settings = localSession.Query<Settings>().First();
			var path = settings.MapPath("Waybills");
			var files = Directory.GetFiles(path).Select(Path.GetFileName);
			Assert.That(files, Contains.Item(Path.GetFileName(file)));
			session.Refresh(sendLog);
			Assert.IsTrue(sendLog.Committed);
			Assert.IsTrue(sendLog.FileDelivered);
			Assert.IsTrue(sendLog.DocumentDelivered);
		}

		[Test]
		public void Import_after_update()
		{
			File.Copy(Directory.GetFiles(serviceConfig.ResultPath).Last(), Tasks.ArchiveFile);
			using(var file = new ZipFile(Tasks.ArchiveFile))
				file.ExtractAll(Tasks.ExtractPath);

			localSession.CreateSQLQuery("delete from offers").ExecuteUpdate();
			Tasks.Import(null, token, progress);
			Assert.That(localSession.Query<Offer>().Count(), Is.GreaterThan(0));
		}

		[Test]
		public void Clean_after_import()
		{
			File.WriteAllBytes(Path.Combine(serviceConfig.UpdatePath, "updater.exe"), new byte[0]);
			File.WriteAllText(Path.Combine(serviceConfig.UpdatePath, "version.txt"), "99.99.99.99");

			task.Start();
			task.Wait();
			Assert.That(task.Result, Is.EqualTo(UpdateResult.UpdatePending));

			File.Delete(Path.Combine(serviceConfig.UpdatePath, "updater.exe"));
			File.Delete(Path.Combine(serviceConfig.UpdatePath, "version.txt"));

			task = CreateTask(new UpdateCommand(Tasks.ArchiveFile, Tasks.ExtractPath, Tasks.RootPath));
			task.Start();
			task.Wait();
			Assert.That(task.Result, Is.EqualTo(UpdateResult.OK));
		}

		private Task<UpdateResult> CreateTask<T>(T command) where T : RemoteCommand
		{
			command.Token = token;
			command.Progress = progress;
			command.BaseUri = uri;
			return new Task<UpdateResult>(t => command.Run(), token);
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
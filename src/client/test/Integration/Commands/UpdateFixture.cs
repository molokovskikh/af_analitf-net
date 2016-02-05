using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using System.Web.Http.SelfHost;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Orders;
using AnalitF.Net.Service.Models;
using Common.NHibernate;
using Common.Tools;
using Ionic.Zip;
using log4net;
using log4net.Config;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;
using Test.Support;
using Test.Support.log4net;
using Test.Support.Suppliers;
using LineResultStatus = AnalitF.Net.Client.Models.LineResultStatus;
using Promotion = AnalitF.Net.Client.Models.Promotion;
using Reject = AnalitF.Net.Client.Test.Fixtures.Reject;

namespace AnalitF.Net.Client.Test.Integration.Commands
{
	[TestFixture]
	public class UpdateFixture : MixedFixture
	{
		private bool restoreUser;
		private bool revertToDefaults;

		[SetUp]
		public void Setup()
		{
			revertToDefaults = false;
			restoreUser = false;
			DbHelper.RestoreData(localSession);
		}

		[TearDown]
		public void Teardown()
		{
			DbHelper.RestoreData(localSession);
			if (restoreUser) {
				session.Flush();
				var user = localSession.Query<User>().First();
				session.CreateSQLQuery("update Customers.Users set Login = Id;" +
					"update Customers.Users set Login = :login where Id = :id")
					.SetParameter("login", ServerFixture.DebugLogin())
					.SetParameter("id", user.Id)
					.ExecuteUpdate();
			}
			else if (revertToDefaults) {
				var user = ServerUser();
				user.UseAdjustmentOrders = false;
			}
		}

		[Test]
		public void Import()
		{
			localSession.CreateSQLQuery("delete from offers").ExecuteUpdate();
			var user = localSession.Query<User>().First();
			user.LastSync = null;

			var command = new UpdateCommand();
			Run(command);
			Assert.AreEqual("Обновление завершено успешно.", command.SuccessMessage);
			Assert.That(localSession.Query<Offer>().Count(), Is.GreaterThan(0));
			Assert.That(localSession.Query<Offer>().Count(x => x.BarCode != null), Is.GreaterThan(0));
			Assert.That(localSession.Query<Offer>().Count(x => x.Properties != ""), Is.GreaterThan(0));
			Assert.That(localSession.Query<Offer>().Count(x => x.NDS != null), Is.GreaterThan(0));

			var productId = localSession.Query<Offer>()
				.Where(o => !o.Junk).GroupBy(o => o.ProductId)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.First();
			var minCostCount = localSession.Query<MinCost>().Count();
			Assert.That(minCostCount, Is.GreaterThan(0));
			var cost = localSession.Query<MinCost>().First(m => m.ProductId == productId);
			Assert.IsNotNull(cost.Catalog, "product id = {0}", productId);
			Assert.IsNotNull(cost.NextCost, "product id = {0}", productId);
			Assert.That(localSession.Query<Offer>().Count(o => o.Exp != null), Is.GreaterThan(0));
		}

		[Test]
		public void Import_version_update()
		{
			File.WriteAllBytes(Path.Combine(serviceConfig.RtmUpdatePath, "updater.exe"), new byte[] { 0x00 });
			File.WriteAllText(Path.Combine(serviceConfig.RtmUpdatePath, "version.txt"), "99.99.99.99");

			var result = Run(new UpdateCommand());

			Assert.That(result, Is.EqualTo(UpdateResult.UpdatePending));
		}

		[Test]
		public void Sent_price_settings_changes()
		{
			restoreUser = true;

			SampleData.CreateUser(session, ServerFixture.DebugLogin());

			Run(new UpdateCommand());

			var price = localSession.Query<Price>().First(p => p.PositionCount > 0);
			Assert.That(price.Active, Is.True);
			Assert.That(price.PositionCount, Is.GreaterThan(0));
			price.Active = false;
			//данные хранятся с точностью до секунды, в тестах операция может быть выполнена за одну секунду тогда
			//комманда будет думать что синхронизировать нечего
			localSession.Refresh(settings);
			settings.LastUpdate = settings.LastUpdate.Value.AddSeconds(-1);

			Run(new UpdateCommand());

			localSession.Refresh(price);
			var user = ServerUser();
			Assert.That(price.Active, Is.False, "прайс {0}, пользователь {1}", price.Id, user.Id);
			Assert.That(price.PositionCount, Is.EqualTo(0));
			var offersCount = localSession.Query<Offer>().Count(o => o.Price == price);
			Assert.That(offersCount, Is.EqualTo(0));

			var priceSettings = session
				.CreateSQLQuery("select * from Customers.UserPrices" +
					" where UserId = :userId and PriceId = :priceId and RegionId = :regionId")
				.SetParameter("userId", user.Id)
				.SetParameter("priceId", price.Id.PriceId)
				.SetParameter("regionId", price.Id.RegionId)
				.List();
			Assert.That(priceSettings.Count, Is.EqualTo(0));
		}

		[Test]
		public void Send_logs()
		{
			File.WriteAllText(Path.Combine(FileHelper.MakeRooted("."), "AnalitF.Net.Client.log"), "123");

			Run(new UpdateCommand());

			var user = ServerUser();
			var text = session.CreateSQLQuery("select Text from Logs.ClientAppLogs" +
				" where UserId = :userId and CreatedOn >= :date")
				.SetParameter("userId", user.Id)
				.SetParameter("date", begin)
				.UniqueResult<string>();
			Assert.That(text, Is.EqualTo("123"));
		}

		[Test]
		public void Print_send_orders()
		{
			settings.PrintOrdersAfterSend = true;
			MakeOrderClean(address);

			var command = new SendOrders(address);
			Run(command);

			var results = command.Results.ToArray();
			Assert.AreEqual(1, results.Length);
			Assert.IsNotNull(((PrintResult)results[0]).Paginator);
		}

		[Test]
		public void Version_update()
		{
			File.WriteAllBytes(Path.Combine(serviceConfig.RtmUpdatePath, "updater.exe"), new byte[] { 0x00 });
			File.WriteAllText(Path.Combine(serviceConfig.RtmUpdatePath, "version.txt"), "99.99.99.99");

			var updateCommand = new UpdateCommand();
			updateCommand.Clean = false;
			var result1 = Run(updateCommand);
			Assert.That(result1, Is.EqualTo(UpdateResult.UpdatePending));

			localSession.CreateSQLQuery("delete from offers").ExecuteUpdate();
			var command1 = new UpdateCommand();
			command1.Configure(settings, clientConfig);
			command1.Process(() => {
				command1.Import();
				return UpdateResult.OK;
			});
			Assert.That(localSession.Query<Offer>().Count(), Is.GreaterThan(0));
		}

		[Test]
		public void Clean_after_import()
		{
			File.WriteAllBytes(Path.Combine(serviceConfig.RtmUpdatePath, "updater.exe"), new byte[] { 0x00 });
			File.WriteAllText(Path.Combine(serviceConfig.RtmUpdatePath, "version.txt"), "99.99.99.99");

			var result1 = Run(new UpdateCommand());
			Assert.That(result1, Is.EqualTo(UpdateResult.UpdatePending));

			File.Delete(Path.Combine(serviceConfig.RtmUpdatePath, "updater.exe"));
			File.Delete(Path.Combine(serviceConfig.RtmUpdatePath, "version.txt"));

			var result2 = Run(new UpdateCommand());
			Assert.That(result2, Is.EqualTo(UpdateResult.OK));
		}

		[Test]
		public void Open_reject()
		{
			var fixture = Fixture<Reject>();
			var command1 = new UpdateCommand();
			Run(command1);

			Assert.AreEqual(
				String.Format(@"var\client\АналитФАРМАЦИЯ\Отказы\{0}_{1}(test).txt",
					fixture.Document.Id,
					fixture.Document.Supplier.Name),
				command1.Results.OfType<OpenResult>().Select(r => FileHelper.RelativeTo(r.Filename, "var")).Implode());
		}

		[Test]
		public void Freeze_order_without_offers()
		{
			var order = MakeOrderClean();

			var newOffer = new Offer(order.Price, 150);
			var random = Generator.Random();
			newOffer.Id.OfferId += (ulong)random.First();
			var catalog = localSession.Query<Catalog>().First(c => !c.HaveOffers);
			newOffer.ProducerSynonym = catalog.FullName;
			newOffer.ProductId = catalog.Id;
			newOffer.CatalogId = catalog.Id;
			newOffer.ProducerSynonymId = (uint?)random.First();
			localSession.Save(newOffer);

			order.TryOrder(newOffer, 1);

			var cmd = new UpdateCommand();
			Run(cmd);

			var text = cmd.Results.OfType<DialogResult>()
				.Select(r => (TextDoc)((DocModel<TextDoc>)r.Model).Model)
				.Select(m => m.Text)
				.First();

			localSession.Clear();

			Assert.That(text, Is.StringContaining("предложение отсутствует"));
			order = localSession.Load<Order>(order.Id);
			Assert.IsTrue(order.Frozen);
			Assert.AreEqual(1, order.Lines.Count);
			var orders = localSession.Query<Order>().ToList();
			Assert.AreEqual(2, orders.Count);
			var newOrder = orders.First(o => o.Id != order.Id);
			Assert.AreEqual(1, newOrder.Lines.Count);
		}

		[Test]
		public void Make_order_correction()
		{
			revertToDefaults = true;

			var user = ServerUser();
			user.UseAdjustmentOrders = true;

			var changedOrder = MakeOrderClean();
			var orderLine = changedOrder.Lines[0];
			var newCost = Math.Max(
				Math.Round(orderLine.Cost + orderLine.Cost * (decimal)Generator.RandomDouble().First(), 2),
				orderLine.Cost + 0.01m);
			session.CreateSQLQuery("update Farm.CoreCosts set cost = :cost where Core_Id = :id")
				.SetParameter("id", orderLine.OfferId.OfferId)
				.SetParameter("cost", newCost)
				.ExecuteUpdate();
			var offer = localSession.Query<Offer>().First(o => o.Price.Id.PriceId != changedOrder.Price.Id.PriceId);
			var normalOrder = MakeOrder(offer: offer);

			var command = new SendOrders(address);
			Run(command);

			localSession.Clear();
			normalOrder = localSession.Get<Order>(normalOrder.Id);
			Assert.IsNotNull(normalOrder);
			Assert.That(normalOrder.SendError, Is.Null.Or.Empty,
				normalOrder.Lines.Implode(l => l.LongSendError));

			changedOrder = localSession.Get<Order>(changedOrder.Id);
			Assert.IsNotNull(changedOrder);
			Assert.AreEqual("В заказе обнаружены позиции с измененной ценой или количеством", changedOrder.SendError);
			var line = changedOrder.Lines[0];
			Assert.AreEqual("имеется различие в цене препарата", line.SendError);
			Assert.AreEqual(newCost, line.NewCost);
			Assert.AreEqual(LineResultStatus.CostChanged, line.SendResult);

			var dialog = command.Results.OfType<DialogResult>().First();
			Assert.IsInstanceOf<Correction>(dialog.Model);
		}

		[Test]
		public void Load_mails()
		{
			var fixture = new CreateMail {
				IsSpecial = true,
			};
			Fixture(fixture);

			var command = new UpdateCommand();
			Run(command);

			var mails = localSession.Load<Mail>(fixture.Mail.Id);
			var attachment = mails.Attachments[0];
			var open = command.Results.OfType<OpenResult>().First();

			Assert.IsTrue(attachment.IsDownloaded);
			Assert.IsTrue(File.Exists(attachment.LocalFilename), attachment.LocalFilename);
			Assert.That(attachment.LocalFilename, Is.StringEnding($@"attachments\{attachment.Id}.txt"));
			Assert.AreEqual(Path.GetFullPath(open.Filename), attachment.LocalFilename);
			session.Refresh(fixture.Log);
			Console.WriteLine(fixture.Log.Id);
			Assert.IsTrue(fixture.Log.Committed);
		}

		[Test]
		public void Load_promotions()
		{
			var fixture = Fixture<CreatePromotion>();
			Run(new UpdateCommand());

			Assert.That(localSession.Query<Promotion>().Count(), Is.GreaterThan(0));
			var promotion = localSession.Get<Promotion>(fixture.Promotion.Id);
			promotion.Init(clientConfig);
			Assert.IsTrue(File.Exists(promotion.LocalFilename));
		}

		[Test]
		public void Load_delay_of_payment()
		{
			Fixture<CreateDelayOfPayment>();
			Run(new UpdateCommand());

			var user = localSession.Query<User>().First();
			Assert.IsTrue(user.IsDelayOfPaymentEnabled);
			Assert.IsTrue(user.ShowSupplierCost);
			localSession.Refresh(settings);
			Assert.AreEqual(DateTime.Today, settings.LastLeaderCalculation);
			Assert.That(localSession.Query<DelayOfPayment>().Count(), Is.GreaterThan(0));
		}

		[Test]
		public void Calculate_buying_matrix_status()
		{
			var offer1 = localSession.Query<Offer>().First();
			var offer2 = localSession.Query<Offer>().First(o => o.ProductId != offer1.ProductId);
			var fixture = new CreateMatrix();
			fixture.Denied = new[] { offer1.ProductId };
			fixture.Warning = new[] { offer2.ProductId };
			var data = session.Load<AnalitfNetData>(ServerUser().Id);
			data.LastUpdateAt = DateTime.MinValue;
			Fixture(fixture);

			Run(new UpdateCommand());

			localSession.Refresh(offer1);
			localSession.Refresh(offer2);
			Assert.AreEqual(BuyingMatrixStatus.Denied, offer1.BuyingMatrixType);
			Assert.AreEqual(BuyingMatrixStatus.Warning, offer2.BuyingMatrixType);
		}

		[Test]
		public void Export_schedule()
		{
			var fixture = new CreateSchedule {
				Schedules = new[] { new TimeSpan(14, 0, 0) }
			};
			Fixture(fixture);
			Run(new UpdateCommand());

			var schedules = localSession.Query<Schedule>().ToArray();
			Assert.AreEqual(1, schedules.Length);
			Assert.AreEqual(new TimeSpan(14, 0, 0), schedules[0].UpdateAt);
		}

		[Test]
		public void Notify_on_awaited()
		{
			SimpleFixture.CreateCleanAwaited(localSession);

			var cmd = new UpdateCommand();
			Run(cmd);

			var model = ((PostUpdate)((DialogResult)cmd.Results[0]).Model);
			Assert.That(model.Text, Is.StringContaining("появились препараты, которые включены Вами в список ожидаемых позиций"));
			Assert.IsTrue(model.IsAwaited);
		}

		[Test]
		public void Load_history()
		{
			var priceId = localSession.Query<Offer>().First().Price.Id.PriceId;
			var user = ServerUser();
			var activePrices = user.GetActivePricesNaked(session);
			var price = activePrices.Where(p => p.Id.PriceId == priceId)
				.Concat(activePrices)
				.First()
				.Price;
			var order = new TestOrder(user, price) {
				Submited = true,
				Processed = true
			};
			var offer = session.Query<TestCore>().First(c => c.Price == price);
			order.AddItem(offer, 1);
			session.Save(order);
			localSession.DeleteEach<SentOrder>();

			var cmd = new UpdateCommand {
				SyncData = "OrderHistory"
			};
			Run(cmd);

			Assert.AreEqual("Загрузка истории заказов завершена успешно.", cmd.SuccessMessage);
			Assert.That(localSession.Query<SentOrder>().Count(), Is.GreaterThan(0));
			var localOrder = localSession.Query<SentOrder>().First(o => o.ServerId == order.Id);
			Assert.AreEqual(1, localOrder.Lines.Count);
			Assert.AreEqual(1, localOrder.LinesCount);
			Assert.That(localOrder.Lines[0].ResultCost, Is.GreaterThan(0));
		}

		[Test]
		public void Clean_offers()
		{
			var supplier = TestSupplier.CreateNaked(session);
			supplier.CreateSampleCore(session);
			var serverUser = ServerUser();
			serverUser.Client.MaintainIntersection(session);

			Run(new UpdateCommand());
			var price = localSession.Query<Price>().First(p => p.SupplierId == supplier.Id);
			var oldOffer = localSession.Query<Offer>().First(o => o.Price == price);

			session.BeginTransaction();
			var testPrice = supplier.Prices[0];
			session.CreateSQLQuery("delete from Farm.Core0 where PriceCode = :priceId")
				.SetParameter("priceId", testPrice.Id)
				.ExecuteUpdate();
			session.Clear();
			supplier = session.Load<TestSupplier>(supplier.Id);
			supplier.CreateSampleCore(session);
			session.CreateSQLQuery("update Usersettings.AnalitFReplicationInfo set ForceReplication = 1 where UserId = :userId and FirmCode = :supplierId")
				.SetParameter("supplierId", testPrice.Supplier.Id)
				.SetParameter("userId", serverUser.Id)
				.ExecuteUpdate();

			Run(new UpdateCommand());

			localSession.Clear();
			var newOffer = localSession.Query<Offer>().FirstOrDefault(o => o.Price == price);
			oldOffer = localSession.Get<Offer>(oldOffer.Id);
			Assert.IsNull(oldOffer);
			Assert.IsNotNull(newOffer);
		}

		[Test, Ignore("тест в данной ветке не актуален")]
		public void Migrate()
		{
			var priceId = localSession.Query<Price>().First().Id.PriceId;
			var supplierId = localSession.Query<Supplier>().First().Id;
			var addressId = localSession.Query<Address>().First().Id;
			Directory.GetFiles(".", "*.txt").Each(File.Delete);
			FileHelper.InitDir("in\\update");
			localSession.Clear();
			DbHelper.Drop();
			using (var sanityCheck = new SanityCheck(clientConfig))
				sanityCheck.Check(true);

			using (var cleaner = new FileCleaner()) {
				var cmd = new UpdateCommand();
				cmd.Configure(settings, clientConfig);
				cmd.Process(() => {
					cmd.Download();
					var dir = Directory.CreateDirectory("in\\update");
					cleaner.WatchDir(dir.FullName);
					new DirectoryInfo(clientConfig.UpdateTmpDir).EnumerateFiles().Each(x => {
						x.MoveTo(Path.Combine(dir.FullName, x.Name));
					});
					//идентфикаторы в тестовых данных
					return UpdateResult.OK;
				});

				cmd = new UpdateCommand();
				cmd.Configure(settings, clientConfig);
				cmd.Process(() => {
					new DirectoryInfo("../../Assets/").EnumerateFiles().Each(x => cleaner.Watch(x.CopyTo(x.Name, true).FullName));
					cmd.Migrate();
					return UpdateResult.OK;
				});
			}
			//идентификаторы не совпадают тк данные для переноса статичные, подделываем id для проверки
			localSession.CreateSQLQuery(@"
update Prices set PriceId = 7537 where PriceId = :priceId;
update Suppliers set Id = 234 where Id = :supplierId;
update Addresses set Id =  2575 where Id = :addressId")
				.SetParameter("priceId", priceId)
				.SetParameter("supplierId", supplierId)
				.SetParameter("addressId", addressId)
				.ExecuteUpdate();

			settings = localSession.Query<Settings>().First();
			Assert.IsNotNull(settings.Password);
			Assert.AreEqual(Taxation.Nds, settings.Waybills[0].Taxation);
			var map = localSession.Query<DirMap>().First(x => x.Supplier.Id == 18089);
			Assert.AreEqual(".\\Загрузка\\Предельные цены производителей", map.Dir);

			var order = localSession.Query<Order>().First();
			Assert.IsNotNull(order.Price);
			Assert.IsNotNull(order.Address);
			Assert.That(order.Lines[0].ResultCost, Is.GreaterThan(0));
			Assert.That(order.Lines[0].Producer, Is.Not.Null.Or.Empty);

			var sentOrder = localSession.Query<SentOrder>().First();
			Assert.IsNotNull(sentOrder.Price);
			Assert.IsNotNull(sentOrder.Address);
			Assert.That(sentOrder.Lines[0].ResultCost, Is.GreaterThan(0));
			Assert.That(sentOrder.Lines[0].Producer, Is.Not.Null.Or.Empty);

			var waybill = localSession.Query<Waybill>().First(x => x.Id == 39153110);
			Assert.IsNotNull(waybill.Supplier);
			Assert.IsNotNull(waybill.Address);
			var line = waybill.Lines.FirstOrDefault(x => x.SerialNumber == "10891996");
			Assert.AreEqual(35, line.MaxRetailMarkup);
			Assert.AreEqual(678.50, line.RetailCost);
			Assert.AreEqual(35, line.RetailMarkup);
			Assert.AreEqual(35, line.RealRetailMarkup);

			line = waybill.Lines.FirstOrDefault(x => x.SerialNumber == "10137353"
				&& x.Product.Contains("Ацетилсалициловой"));
			Assert.AreEqual(29.99m, line.RetailMarkup);
			Assert.AreEqual(70.21m, line.RealRetailMarkup);
			Assert.AreEqual(613.70m, line.RetailCost);

			line = waybill.Lines.FirstOrDefault(x => x.SerialNumber == "017022014");
			Assert.AreEqual(21.36m, line.RetailMarkup);
			Assert.AreEqual(49.99m, line.RealRetailMarkup);
			Assert.AreEqual(540.80m, line.RetailCost);

			line = waybill.Lines.FirstOrDefault(x => x.SerialNumber == "156014");
			Assert.AreEqual(77.63m, line.RetailMarkup);
			Assert.AreEqual(82.03m, line.RealRetailMarkup);
			Assert.AreEqual(500m, line.RetailCost);
		}

		[Test]
		public void Select_host()
		{
			var emptyServerUrl = String.Format("http://localhost:{0}", new Random().Next(10000, 20000));
			var cfg = new HttpSelfHostConfiguration(emptyServerUrl);
			cfg.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
			var server = new HttpSelfHostServer(cfg);
			disposable.Add(server);
			server.OpenAsync().Wait();

			clientConfig = clientConfig.Clone();
			var normalServerUrl = new UriBuilder(clientConfig.BaseUrl) {
				Host = "127.0.0.1"
			}.ToString();
			clientConfig.AltUri = normalServerUrl + "," + emptyServerUrl;
			var cmd = new UpdateCommand();
			disposable.Add(cmd);
			cmd.Configure(settings, clientConfig);
			Assert.AreEqual(normalServerUrl, cmd.ConfigureHttp().ToString());
		}
	}
}
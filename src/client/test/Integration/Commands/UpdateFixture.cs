using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Web.Http;
using System.Web.Http.SelfHost;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Orders;
using AnalitF.Net.Service.Models;
using Common.NHibernate;
using Common.Tools;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;
using Test.Support;
using Test.Support.log4net;
using Test.Support.Suppliers;
using CreateWaybill = AnalitF.Net.Client.Test.Fixtures.CreateWaybill;
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
		private uint userId;

		[SetUp]
		public void Setup()
		{
			revertToDefaults = false;
			restoreUser = false;
			userId = localSession.Query<User>().Select(x => x.Id).First();
			DbHelper.RestoreData(localSession);
		}

		[TearDown]
		public void Teardown()
		{
			if (restoreUser) {
				session.Flush();
				session.CreateSQLQuery("update Customers.Users set Login = Id;" +
					"update Customers.Users set Login = :login where Id = :id")
					.SetParameter("login", ServerFixture.DebugLogin())
					.SetParameter("id", userId)
					.ExecuteUpdate();
			}
			else if (revertToDefaults) {
				var user = ServerUser();
				user.UseAdjustmentOrders = false;
			}
			session.Flush();
		}

		[Test]
		public void Import()
		{
			localSession.CreateSQLQuery("delete from offers").ExecuteUpdate();
			var user = localSession.Query<User>().First();
			user.LastSync = null;

			var command = new UpdateCommand();
			Run(command);
			DbMaintain.UpdateLeaders(localStateless);

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
			//команда будет думать что синхронизировать нечего
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
			var logfile = Path.Combine(FileHelper.MakeRooted("."), "AnalitF.Net.Client.log");
			File.WriteAllText(logfile, "123");

			Run(new UpdateCommand());

			var user = ServerUser();
			var text = session.CreateSQLQuery("select Text from Logs.ClientAppLogs" +
				" where UserId = :userId and CreatedOn >= :date")
				.SetParameter("userId", user.Id)
				.SetParameter("date", begin)
				.UniqueResult<string>();
			Assert.That(text, Is.EqualTo("123"));
			Assert.IsFalse(File.Exists(logfile), $"Файл логов должен быть удален {logfile}");
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
			var user = localSession.Query<User>().First();
			user.LastSync = null;
			File.WriteAllBytes(Path.Combine(serviceConfig.RtmUpdatePath, "updater.exe"), new byte[] { 0x00 });
			File.WriteAllText(Path.Combine(serviceConfig.RtmUpdatePath, "version.txt"), "99.99.99.99");
			localSession.CreateSQLQuery("delete from offers").ExecuteUpdate();

			//сначала будут загружены только бинарники
			Assert.AreEqual(UpdateResult.UpdatePending, Run(new UpdateCommand()));
			Assert.AreEqual(0, localSession.Query<Offer>().Count());

			//теперь только данные
			Assert.AreEqual(UpdateResult.OK, Run(new UpdateCommand {
				SyncData = "NoBin"
			}));
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
				$@"var\client\АналитФАРМАЦИЯ\Отказы\{fixture.Document.Id}_{fixture.Document.Supplier.Name}(test).txt",
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

			Assert.That(text, Does.Contain("предложение отсутствует"));
			order = localSession.Load<Order>(order.Id);
			Assert.IsTrue(order.Frozen);
			Assert.AreEqual(1, order.Lines.Count);
			var orders = localSession.Query<Order>().ToList();
			Assert.AreEqual(2, orders.Count);
			var newOrder = orders.First(o => o.Id != order.Id);
			Assert.AreEqual(1, newOrder.Lines.Count);
		}

		[Test]
		public void Freeze_old_orders()
		{
			var order = MakeOrderClean();
			order.CreatedOn = order.CreatedOn.AddDays(-10);
			localSession.Save(order);

			var cmd = new UpdateCommand();
			Run(cmd);

			var text = cmd.Results.OfType<MessageResult>().First().Message;

			localSession.Clear();
			order = localSession.Load<Order>(order.Id);

			Assert.IsTrue(order.Frozen);
			Assert.That(text, Does.Contain("В архиве заказов обнаружены заказы, сделанные более 1 недели назад. Данные заказы были заморожены."));
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
		public void Load_ProducerPromotions()
		{
			var fixture = Fixture<CreateProducerPromotion>();
			Run(new UpdateCommand());

			Assert.That(localSession.Query<Client.Models.ProducerPromotion>().Count(), Is.GreaterThan(0));

			var producerPromotion = localSession.Get<Client.Models.ProducerPromotion>(fixture.ProducerPromotion.Id);
			producerPromotion.Init(clientConfig);

			Assert.AreEqual("Тестовая промоакция производителя", producerPromotion.Name);
		}

		[Test]
		public void Load_delay_of_payment()
		{
			settings.LastLeaderCalculation = DateTime.Today.AddDays(-1);
			Fixture<CreateDelayOfPayment>();
			Run(new UpdateCommand());
			DbMaintain.UpdateLeaders(localStateless);

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
			Assert.That(model.Text, Does.Contain("появились препараты, которые включены Вами в список ожидаемых позиций"));
			Assert.IsTrue(model.IsAwaited);
		}

		[Test]
		public void Notify_on_rejected_in_stock()
		{
			var reject = localSession.Query<Client.Models.Reject>().FirstOrDefault();
			if (reject == null) {
				reject = new Client.Models.Reject();
				localSession.Save(reject);
				localSession.Flush();
			}
			var fix = new LocalWaybill();
			fix.Execute(localSession);
			var line = fix.Waybill.Lines.First();
			line.RejectId = reject.Id;
			localSession.Update(fix.Waybill);
			var stock = new Stock() {WaybillLineId = line.Id, Quantity = 1};
			localSession.Save(stock);
			localSession.Flush();

			var cmd = new UpdateCommand();
			Run(cmd);

			var model = ((PostUpdate)((DialogResult)cmd.Results[0]).Model);
			Assert.That(model.Text, Does.Contain("на складе присутствуют забракованные препараты"));
			Assert.IsTrue(model.IsRejectedOnStock);
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
		public void Load_WaybillsHistory()
		{
			var cmd = new UpdateCommand {
				SyncData = "WaybillHistory"
			};

			Assert.AreEqual(UpdateResult.SilentOk, Run(cmd));
			Assert.AreEqual("Загрузка истории документов завершена успешно.", cmd.SuccessMessage);

			var fields = cmd.GetType().GetRuntimeFields();
			uint requestId = 0;
			foreach (FieldInfo field in fields) {
				if (field.Name.Match("requestId")) {
					requestId = (uint)field.GetValue(cmd);
				}
			}
			var localWaybill = session.Query<RequestLog>().Where(x => x.Id == requestId);
			Assert.AreEqual("WaybillsController", localWaybill.First().UpdateType);
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
			var emptyServerUrl = InitHelper.RandomPort();
			var cfg = new HttpSelfHostConfiguration(emptyServerUrl) {
				IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always,
				HostNameComparisonMode = HostNameComparisonMode.Exact
			};
			var server = new HttpSelfHostServer(cfg);
			disposable.Add(server);
			server.OpenAsync().Wait();

			clientConfig = clientConfig.Clone();
			var normalServerUrl = clientConfig.BaseUrl;
			clientConfig.AltUri = emptyServerUrl + "," + normalServerUrl;
			var cmd = new UpdateCommand();
			disposable.Add(cmd);
			cmd.Configure(settings, clientConfig);
			Assert.AreEqual(normalServerUrl, cmd.ConfigureHttp().ToString());
		}

		[Test]
		public void Reconfigure_host()
		{
			var normal = clientConfig.BaseUrl;
			var broken = InitHelper.RandomPort();
			clientConfig = clientConfig.Clone();
			clientConfig.BaseUrl = broken;
			clientConfig.AltUri = broken + "," + normal;

			var init = InitHelper.InitService(broken, new Service.Config.Config {
				RootPath = cleaner.RandomDir(),
				Environment = "Development",
				InjectedFault = "Test error"
			}).Result;
			disposable.Add(init.Item1);

			var result = Run(new UpdateCommand());
			Assert.That(result, Is.EqualTo(UpdateResult.SilentOk).Or.EqualTo(UpdateResult.OK));
		}

		[Test]
		public void Do_not_load_order_on_inactive_address()
		{
			var createAddress = new CreateAddress();
			Fixture(createAddress);
			Run(new UpdateCommand());
			Fixture(new UnconfirmedOrder());
			Fixture(new UnconfirmedOrder {
				Address = createAddress.Address,
				Clean = false
			});
			var config = localSession.Query<AddressConfig>().First(x => x.Address.Id == createAddress.Address.Id);
			Assert.IsTrue(config.IsActive);
			config.IsActive = false;
			Run(new UpdateCommand());
			var orders = localSession.Query<Order>().ToArray();
			Assert.AreEqual(1, orders.Length);
		}

		[Test]
		public void Deactive_all_addresses()
		{
			Fixture(new UnconfirmedOrder());
			var config = localSession.Query<AddressConfig>().First();
			Assert.IsTrue(config.IsActive);
			config.IsActive = false;
			Run(new UpdateCommand());
			var orders = localSession.Query<Order>().ToArray();
			Assert.AreEqual(0, orders.Length);
		}

		[Test]
		public void Show_user_update_message()
		{
			Fixture(new CreateMessageUpdateInfo());
			var cmd = new UpdateCommand();
			Run(cmd);
			var result = (MessageResult)cmd.Results[0];
			Assert.AreEqual(cmd.Results.Count, 1);
			Assert.That(result.Message, Does.Contain("Test Message"));
			cmd = new UpdateCommand();
			Run(cmd);
			Assert.AreEqual(cmd.Results.Count, 0);
		}

		[Test]
		public void Show_user_update_message_waybills()
		{
			Fixture(new CreateMessageUpdateInfo());
			localSession.Query<User>().First().Message = "Test Message";
			var cmd = new UpdateCommand();
			Run(cmd);
			Assert.AreEqual(1, cmd.Results.Count);
			var result = (MessageResult)cmd.Results[0];
			Assert.That(result.Message, Does.Contain("Test Message"));
			cmd = new UpdateCommand();
			cmd.SyncData = "Waybills";
			Run(cmd);
			Assert.AreEqual(0, cmd.Results.Count);
		}

		[Test, Ignore("Тест содержит дефекты")]
		public void Update_address()
		{
			var fixtureAddressChange = Fixture<CreateAddress>();
			var fixtureAddressNotChange = Fixture<CreateAddress>();

			User user = new User();
			Address AddressChange = new Address("тестовый адрес доставки до изменения");
			WaybillSettings WaybillSettingsChange = new WaybillSettings(user, AddressChange);

			Address AddressNotChange = new Address("тестовый адрес доставки до изменения");
			WaybillSettings WaybillSettingsNotChange = new WaybillSettings(user, AddressNotChange);


			using (var transaction = localSession.BeginTransaction())
			{
				AddressChange.Id = fixtureAddressChange.Address.Id;
				AddressNotChange.Id = fixtureAddressNotChange.Address.Id;
				WaybillSettingsChange.Address = "тестовый адрес доставки до изменения";
				WaybillSettingsNotChange.Address = "тестовый адрес доставки после ручного изменения";

				localSession.Save(AddressChange);
				localSession.Save(AddressNotChange);
				localSession.Save(WaybillSettingsChange);
				localSession.Save(WaybillSettingsNotChange);
				transaction.Commit();
			}

			localSession.Clear();
			Run(new UpdateCommand());

			WaybillSettingsChange = localSession.Query<WaybillSettings>().FirstOrDefault(x => x.BelongsToAddress.Id == AddressChange.Id);
			Assert.AreEqual(fixtureAddressChange.Address.Value, WaybillSettingsChange.Address);
			WaybillSettingsNotChange = localSession.Query<WaybillSettings>().FirstOrDefault(x => x.BelongsToAddress.Id == AddressNotChange.Id);
			Assert.AreEqual("тестовый адрес доставки после ручного изменения", WaybillSettingsNotChange.Address);
		}
	}
}
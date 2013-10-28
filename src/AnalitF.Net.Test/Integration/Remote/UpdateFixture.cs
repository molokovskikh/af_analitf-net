using System;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Orders;
using AnalitF.Net.Service.Test.TestHelpers;
using Common.NHibernate;
using Common.Tools;
using Ionic.Zip;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Address = AnalitF.Net.Client.Models.Address;
using Reject = AnalitF.Net.Client.Test.Fixtures.Reject;

namespace AnalitF.Net.Test.Integration.Remote
{
	[TestFixture]
	public class UpdateFixture : IntegrationFixture
	{
		private ISession localSession;
		private Task<UpdateResult> task;
		private CancellationTokenSource cancelletion;
		private BehaviorSubject<Progress> progress;
		private CancellationToken token;

		private bool restoreUser;

		private Service.Config.Config serviceConfig;
		private RemoteCommand command;
		private Settings settings;
		private DateTime begin;
		private Address address;
		private bool revertToDefaults;

		private Client.Config.Config clientConfig;

		[SetUp]
		public void Setup()
		{
			clientConfig = SetupFixture.clientConfig;
			serviceConfig = SetupFixture.serviceConfig;

			begin = DateTime.Now;
			restoreUser = false;

			var files = Directory.GetFiles(".", "*.txt");
			foreach (var file in files) {
				File.Delete(file);
			}

			FileHelper.InitDir(serviceConfig.UpdatePath, clientConfig.RootDir, clientConfig.TmpDir);

			localSession = SetupFixture.Factory.OpenSession();
			cancelletion = new CancellationTokenSource();
			token = cancelletion.Token;
			progress = new BehaviorSubject<Progress>(new Progress());

			command = new UpdateCommand();
			task = CreateTask(command);

			settings = localSession.Query<Settings>().First();
			address = localSession.Query<Address>().First();

			ViewModelFixture.StubWindowManager();
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
			else if (revertToDefaults) {
				var user = ServerUser();
				user.UseAdjustmentOrders = false;
			}
		}

		[Test]
		public void Import()
		{
			localSession.CreateSQLQuery("delete from offers").ExecuteUpdate();

			Update();
			var offers = localSession.CreateSQLQuery("select * from offers").List();
			Assert.AreEqual("Обновление завершено успешно.", command.SuccessMessage);
			Assert.That(offers.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Import_version_update()
		{
			File.WriteAllBytes(Path.Combine(serviceConfig.UpdatePath, "updater.exe"), new byte[0]);
			File.WriteAllText(Path.Combine(serviceConfig.UpdatePath, "version.txt"), "99.99.99.99");

			Update();

			Assert.That(task.Result, Is.EqualTo(UpdateResult.UpdatePending));
		}

		[Test]
		public void Sent_price_settings_changes()
		{
			restoreUser = true;

			SampleData.CreateUser(session);

			var price = localSession.Query<Price>().First(p => p.PositionCount > 0);
			Assert.That(price.Active, Is.True);
			Assert.That(price.PositionCount, Is.GreaterThan(0));
			price.Active = false;

			Update();

			localSession.Refresh(price);
			Assert.That(price.Active, Is.False, price.Id.ToString());
			Assert.That(price.PositionCount, Is.EqualTo(0));
			var offersCount = localSession.Query<Offer>().Count(o => o.Price == price);
			Assert.That(offersCount, Is.EqualTo(0));

			var user = ServerUser();
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
			File.WriteAllText(@"app\AnalitF.Net.Client.log", "123");

			Update();

			var user = ServerUser();
			var text = session.CreateSQLQuery("select Text from Logs.ClientAppLogs" +
				" where UserId = :userId and CreatedOn >= :date")
				.SetParameter("userId", user.Id)
				.SetParameter("date", begin)
				.UniqueResult<string>();
			Assert.That(text, Is.EqualTo("123"));
		}

		[Test]
		public void Send_orders()
		{
			var order = MakeOrderClean();
			var line = order.Lines[0];

			Update(new SendOrders(address));

			Assert.That(localSession.Query<Order>().Count(), Is.EqualTo(0));
			var sentOrders = localSession.Query<SentOrder>().Where(o => o.SentOn >= begin).ToList();
			Assert.That(sentOrders.Count, Is.EqualTo(1));
			Assert.That(sentOrders[0].Lines.Count, Is.EqualTo(1));

			var orders = session.Query<TestOrder>().Where(o => o.WriteTime >= begin).ToList();
			Assert.That(orders.Count, Is.EqualTo(1));
			var resultOrder = orders[0];
			Assert.That(resultOrder.RowCount, Is.EqualTo(1));
			var item = resultOrder.Items[0];
			Assert.That(item.CodeFirmCr, Is.EqualTo(line.ProducerId));
			Assert.That(item.SynonymCode, Is.EqualTo(line.ProductSynonymId));
			Assert.That(item.SynonymFirmCrCode, Is.EqualTo(line.ProducerSynonymId));
		}

		[Test]
		public void Reject_order_by_min_req()
		{
			var offer = localSession.Query<Offer>().First(o => o.Price.SupplierName.Contains("минимальный заказ"));
			var order = MakeOrderClean(address, offer);

			Update(new SendOrders(address));

			var text = command.Results
				.OfType<DialogResult>()
				.Select(d => d.Model)
				.OfType<TextViewModel>()
				.Select(t => t.Text)
				.FirstOrDefault();
			var expected = String.Format("прайс-лист {0} - Поставщик отказал в приеме заказа." +
				" Сумма заказа меньше минимально допустимой." +
				" Минимальный заказ {1:C} заказано {2:C}.", order.Price.Name, 1500, order.Sum);
			Assert.AreEqual(expected, text);
		}

		[Test]
		public void Print_send_orders()
		{
			settings.PrintOrdersAfterSend = true;
			MakeOrderClean(address);

			Update(new SendOrders(address));

			var results = command.Results.ToArray();
			Assert.AreEqual(1, results.Length);
			Assert.IsNotNull(((PrintResult)results[0]).Paginator);
		}

		[Test]
		public void Import_waybill()
		{
			var fixture = LoadFixture<LoadWaybill>();
			var sendLog = fixture.SendLog;
			Update();

			var waybills = localSession.Query<Waybill>().ToList();
			Assert.That(waybills.Count(), Is.GreaterThanOrEqualTo(1));
			Assert.That(waybills[0].Sum, Is.GreaterThan(0));
			Assert.That(waybills[0].RetailSum, Is.GreaterThan(0));

			var path = settings.MapPath("Waybills");
			var files = Directory.GetFiles(path).Select(Path.GetFileName);
			Assert.That(files, Contains.Item(Path.GetFileName(fixture.Filename)));
			session.Refresh(sendLog);
			Assert.IsTrue(sendLog.Committed);
			Assert.IsTrue(sendLog.FileDelivered);
			Assert.IsTrue(sendLog.DocumentDelivered);
		}

		[Test]
		public void Group_waybill()
		{
			settings.GroupWaybillsBySupplier = true;
			settings.OpenWaybills = true;

			var fixture = LoadFixture<LoadWaybill>();
			Update();

			var path = Path.Combine(settings.MapPath("Waybills"), fixture.Waybill.Supplier.Name);
			var files = Directory.GetFiles(path).Select(Path.GetFileName);
			Assert.That(files, Contains.Item("test.txt"));
			var results = command.Results.OfType<OpenResult>().Implode(r => r.Filename);
			Assert.AreEqual(Path.Combine(path, "test.txt"), results);
		}

		[Test]
		public void Import_after_update()
		{
			File.Copy(Directory.GetFiles(serviceConfig.ResultPath).Last(), clientConfig.ArchiveFile);
			using(var file = new ZipFile(clientConfig.ArchiveFile))
				file.ExtractAll(clientConfig.UpdateTmpDir);

			localSession.CreateSQLQuery("delete from offers").ExecuteUpdate();
			command.Config = clientConfig;
			command.Progress = progress;
			command.Process(() => {
				((UpdateCommand)command).Import();
				return UpdateResult.OK;
			});
			Assert.That(localSession.Query<Offer>().Count(), Is.GreaterThan(0));
		}

		[Test]
		public void Clean_after_import()
		{
			File.WriteAllBytes(Path.Combine(serviceConfig.UpdatePath, "updater.exe"), new byte[0]);
			File.WriteAllText(Path.Combine(serviceConfig.UpdatePath, "version.txt"), "99.99.99.99");

			Update();
			Assert.That(task.Result, Is.EqualTo(UpdateResult.UpdatePending));

			File.Delete(Path.Combine(serviceConfig.UpdatePath, "updater.exe"));
			File.Delete(Path.Combine(serviceConfig.UpdatePath, "version.txt"));

			task = CreateTask(new UpdateCommand());
			Update();
			Assert.That(task.Result, Is.EqualTo(UpdateResult.OK));
		}

		[Test]
		public void Open_reject()
		{
			var fixture = LoadFixture<Reject>();
			Update();

			Assert.AreEqual(
				String.Format(@"var\client\АналитФАРМАЦИЯ\Отказы\{0}_{1}(test).txt",
					fixture.Document.Id,
					fixture.Document.Supplier.Name),
				command.Results.OfType<OpenResult>().Select(r => FileHelper.RelativeTo(r.Filename, "var")).Implode());
		}

		[Test]
		public void Freeze_order_without_offers()
		{
			session.DeleteEach<Order>();

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

			order.AddLine(newOffer, 1);

			Update();

			var text = command.Results.OfType<DialogResult>()
				.Select(r => (TextViewModel)r.Model)
				.Select(m => m.Text)
				.First();

			localSession.Clear();

			Assert.That(text, Is.StringContaining("Предложений не найдено"));
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

			Update(new SendOrders(address));

			localSession.Clear();
			normalOrder = localSession.Get<Order>(normalOrder.Id);
			Assert.IsNotNull(normalOrder);
			Assert.IsNullOrEmpty(normalOrder.SendError,
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
		public void Load_only_waybills()
		{
			session.CreateSQLQuery(@"delete from Logs.DocumentSendLogs"
				+ " where UserId = :userId;")
				.SetParameter("userId", ServerUser().Id)
				.ExecuteUpdate();
			((UpdateCommand)command).SyncData = "Waybills";
			((UpdateCommand)command).Clean = false;

			session.Transaction.Commit();
			command.Run();

			var files = ZipHelper.lsZip(clientConfig.ArchiveFile).Implode();
			Assert.AreEqual("Новых файлов документов нет.", command.SuccessMessage);
			Assert.AreEqual("", files);
		}

		private TestUser ServerUser()
		{
			return session.Query<TestUser>().First(u => u.Login == Environment.UserName);
		}

		private void Update(RemoteCommand cmd)
		{
			command = cmd;
			task = CreateTask(cmd);
			Update();
		}

		private T LoadFixture<T>()
		{
			var fixture = (dynamic)Activator.CreateInstance<T>();
			fixture.Config = serviceConfig;
			fixture.Execute(session);
			return fixture;
		}

		private Order MakeOrderClean(Address address = null, Offer offer = null)
		{
			localSession.DeleteEach<Order>();
			return MakeOrder(address, offer);
		}

		private Order MakeOrder(Address address = null, Offer offer = null)
		{
			address = address ?? this.address;
			using (localSession.BeginTransaction()) {
				offer = offer ?? localSession.Query<Offer>().First();
				var order = new Order(offer.Price, address);
				order.AddLine(offer, 1);
				localSession.Save(order);
				return order;
			}
		}

		private Task<UpdateResult> CreateTask<T>(T command) where T : RemoteCommand
		{
			command.Config = clientConfig;
			command.Token = token;
			command.Progress = progress;
			return new Task<UpdateResult>(t => command.Run(), token);
		}

		private void Update()
		{
			localSession.Flush();
			session.Flush();
			if (session.Transaction.IsActive)
				session.Transaction.Commit();

			task.Start();
			task.Wait();
		}
	}
}
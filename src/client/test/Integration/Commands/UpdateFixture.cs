using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Orders;
using AnalitF.Net.Service.Test.TestHelpers;
using AnalitF.Net.Test.Integration;
using Common.NHibernate;
using Common.Tools;
using Ionic.Zip;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Reject = AnalitF.Net.Client.Test.Fixtures.Reject;

namespace AnalitF.Net.Test.Integration.Commands
{
	[TestFixture]
	public class UpdateFixture : MixedFixture
	{
		private bool restoreUser;
		private bool revertToDefaults;

		[SetUp]
		public void Setup()
		{
			restoreUser = false;
		}

		[TearDown]
		public void Teardown()
		{
			Integration.IntegrationSetup.RestoreData(localSession);
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

			var command = new UpdateCommand();
			Run(command);
			var offers = localSession.CreateSQLQuery("select * from offers").List();
			Assert.AreEqual("Обновление завершено успешно.", command.SuccessMessage);
			Assert.That(offers.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Import_version_update()
		{
			File.WriteAllBytes(Path.Combine(serviceConfig.UpdatePath, "updater.exe"), new byte[] { 0x00 });
			File.WriteAllText(Path.Combine(serviceConfig.UpdatePath, "version.txt"), "99.99.99.99");

			var result = Run(new UpdateCommand());

			Assert.That(result, Is.EqualTo(UpdateResult.UpdatePending));
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

			Run(new UpdateCommand());

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
			File.WriteAllText(Path.Combine(clientConfig.RootDir, "AnalitF.Net.Client.log"), "123");

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
		public void Reject_order_by_min_req()
		{
			var offer = localSession.Query<Offer>().First(o => o.Price.SupplierName.Contains("минимальный заказ"));
			var order = MakeOrderClean(address, offer);

			var command = new SendOrders(address);
			Run(command);

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

			var command = new SendOrders(address);
			Run(command);

			var results = command.Results.ToArray();
			Assert.AreEqual(1, results.Length);
			Assert.IsNotNull(((PrintResult)results[0]).Paginator);
		}

		[Test]
		public void Import_after_update()
		{
			var src = Directory.GetFiles(serviceConfig.ResultPath)
				.OrderByDescending(l => new FileInfo(l).LastWriteTime)
				.Last();
			File.Copy(src, clientConfig.ArchiveFile);
			using(var file = new ZipFile(clientConfig.ArchiveFile))
				file.ExtractAll(clientConfig.UpdateTmpDir);

			localSession.CreateSQLQuery("delete from offers").ExecuteUpdate();
			RemoteCommand command1 = new UpdateCommand();
			command1.Config = clientConfig;
			command1.Process(() => {
				((UpdateCommand)command1).Import();
				return UpdateResult.OK;
			});
			Assert.That(localSession.Query<Offer>().Count(), Is.GreaterThan(0));
		}

		[Test]
		public void Clean_after_import()
		{
			File.WriteAllBytes(Path.Combine(serviceConfig.UpdatePath, "updater.exe"), new byte[] { 0x00 });
			File.WriteAllText(Path.Combine(serviceConfig.UpdatePath, "version.txt"), "99.99.99.99");

			var result1 = Run(new UpdateCommand());
			Assert.That(result1, Is.EqualTo(UpdateResult.UpdatePending));

			File.Delete(Path.Combine(serviceConfig.UpdatePath, "updater.exe"));
			File.Delete(Path.Combine(serviceConfig.UpdatePath, "version.txt"));

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

			order.TryOrder(newOffer, 1);

			var command1 = new UpdateCommand();
			Run(command1);

			var text = command1.Results.OfType<DialogResult>()
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

			var command = new SendOrders(address);
			Run(command);

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
		public void Load_mails()
		{
			var fixture = new MailWithAttachment {
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
			Assert.That(attachment.LocalFilename, Is.StringEnding(String.Format(@"attachments\{0}.txt", attachment.Id)));
			Assert.AreEqual(Path.GetFullPath(open.Filename), attachment.LocalFilename);
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
			Assert.IsTrue(user.IsDeplayOfPaymentEnabled);
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
	}
}
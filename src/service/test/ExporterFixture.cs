using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AnalitF.Net.Service.Controllers;
using AnalitF.Net.Service.Models;
using AnalitF.Net.Service.Test.TestHelpers;
using Common.Models;
using Common.Tools;
using Ionic.Zip;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping;
using NUnit.Framework;
using Test.Support;
using Test.Support.Documents;
using Test.Support.Suppliers;
using Test.Support.log4net;
using Test.Support.Logs;

namespace AnalitF.Net.Service.Test
{
	[TestFixture]
	public class ExporterFixture : IntegrationFixture2
	{
		private User user;
		private Exporter exporter;
		private string file;
		private TestClient client;
		private Config.Config config;
		private RequestLog requestLog;

		[SetUp]
		public void Setup()
		{
			config = FixtureSetup.Config;
			client = TestClient.CreateNaked(session);
			session.Save(client);

			if (config.RegulatorRegistryPriceId == 0) {
				var supplier = TestSupplier.CreateNaked(session);
				supplier.CreateSampleCore(session);
				config.RegulatorRegistryPriceId = supplier.Prices[0].Id;
			}
			user = session.Load<User>(client.Users[0].Id);
			FileHelper.InitDir("data", "var/update", "var/update/rtm");
			Directory.CreateDirectory(config.LocalExportPath);
			Directory.GetFiles(config.LocalExportPath).Each(File.Delete);

			file = "data.zip";
			File.Delete(file);
			Init();
		}

		[TearDown]
		public void TearDown()
		{
			FlushAndCommit();
			exporter?.Dispose();
			exporter = null;
		}

		[Test]
		public void Export_with_check_token()
		{
			requestLog.ClientToken = Guid.NewGuid().ToString();
			var settings = session.Load<UserSettings>(user.Id);
			settings.CheckClientToken = true;
			exporter.ExportAll();
		}

		[Test]
		public void Export_update()
		{
			File.WriteAllText("var/update/rtm/version.txt", "1.2");
			File.WriteAllBytes("var/update/rtm/analitf.net.client.exe", new byte[] { 0x00 });

			ExportCompressed();
			var files = ZipHelper.lsZip(file);

			Assert.That(exporter.External[0].Filename, Does.EndWith(@"var\cache\ext-rtm.zip"));
			var extFiles = ZipHelper.lsZip(exporter.External[0].Filename).Implode();
			Assert.That(extFiles, Does.Contain("update/analitf.net.client.exe"));
			Assert.That(files.Implode(), Does.Not.Contains("update/analitf.net.client.exe"));
		}

		[Test]
		public void Export_diff_update()
		{
			File.WriteAllText("var/update/rtm/version.txt", "1.2");
			File.WriteAllBytes("var/update/rtm/analitf.net.client.exe", new byte[] { 0x00 });
			File.WriteAllBytes("var/update/delta-1.1-1.2.zip", new byte[] { 0x00 });

			ExportCompressed();

			Assert.That(exporter.External[0].Filename, Does.EndWith(@"var\update\delta-1.1-1.2.zip"));
		}

		[Test]
		public void Export_meta()
		{
			ExportCompressed();
			var zipEntries = ZipHelper.lsZip(file).Implode();

			Assert.That(File.Exists(file), "{0} не существует", file);
			Assert.That(zipEntries, Does.Contain("Addresses.txt"));
			Assert.That(zipEntries, Does.Contain("Addresses.meta.txt"));

			Assert.That(Path.GetFileName(Directory.GetFiles(exporter.Config.LocalExportPath)[0]),
				Is.EqualTo("1Addresses.txt"));
			Assert.That(Directory.GetFiles("data")[0], Is.EquivalentTo("data\\data.zip"));
			exporter.Dispose();
			Assert.That(Directory.GetFiles(exporter.Config.LocalExportPath), Is.Empty);
		}

		[Test]
		public void Export_news()
		{
			DataMother.News(session);

			ExportCompressed();
			var files = ZipHelper.lsZip(file);
			Assert.IsTrue(files.Any(f => Regex.IsMatch(f, @"newses/\d+.html")), files.Implode());
		}

		[Test]
		public void Export_ads()
		{
			InitAd();

			exporter.ExportAll();
			var files = ListResult();

			Assert.That(files, Does.Contain("ads/2block.gif"));
		}

		[Test]
		public void Ads_sync()
		{
			InitAd();
			FileHelper.Touch(@"ads\Воронеж_1\index.gif");
			exporter.ExportAds();
			exporter.Confirm(new ConfirmRequest(requestLog.Id));
			Assert.AreEqual("ads/2block.gif, ads/index.gif", exporter.Result.Implode(x => x.ArchiveFileName));

			new FileInfo(@"ads\Воронеж_1\index.gif").LastWriteTime = DateTime.Now.AddMinutes(10);
			Init();
			exporter.ExportAds();
			Assert.AreEqual("ads/2block.gif, ads/index.gif", exporter.Result.Implode(x => x.ArchiveFileName));
		}

		[Test]
		public void Export_empty_ad()
		{
			InitAd();

			var settings = session.Load<ClientSettings>(client.Id);
			settings.ShowAdvertising = false;
			exporter.ExportAds();

			var files = ListResult();
			Assert.AreEqual("ads/delete.me", files);
		}

		[Test]
		public void Export_all_offers()
		{
			var supplier = TestSupplier.CreateNaked(session);
			client.MaintainIntersection(session);
			var price = supplier.Prices[0];
			var product = session.Query<TestProduct>().First(p => !p.CatalogProduct.Hidden);
			var synonym = price.AddProductSynonym(product.CatalogProduct.Name, product);
			var core1 = new TestCore(synonym) {
				Quantity = "1500"
			};
			var core2 = new TestCore(synonym) {
				Quantity = "200"
			};
			session.Save(core1);
			session.Save(core2);
			core1.AddCost(100);
			core2.AddCost(150);
			session.Flush();
			session.Save(core1);
			session.Save(core2);

			exporter.Export();

			var offers = exporter.Result.First(t => t.ArchiveFileName.EndsWith("offers.txt"));
			var text = File.ReadAllText(offers.LocalFileName);
			Assert.That(text, Does.Contain(core1.Id.ToString()));
			Assert.That(text, Does.Contain(core2.Id.ToString()));
		}

		[Test]
		public void Export_waybills()
		{
			user.SendWaybills = true;
			var waybillFile = CreateWaybillWithFile().Log.LocalFile;

			exporter.ExportDocs();
			var files = ListResult();
			Assert.AreEqual(files, String.Format("Waybills/{0}, Waybills.meta.txt, Waybills.txt,"
				+ " WaybillLines.meta.txt, WaybillLines.txt, WaybillOrders.meta.txt, WaybillOrders.txt,"
				+ " OrderRejects.meta.txt, OrderRejects.txt, OrderRejectLines.meta.txt, OrderRejectLines.txt,"
				+ " LoadedDocuments.meta.txt, LoadedDocuments.txt",
				Path.GetFileName(waybillFile)));
		}

		[Test]
		public void Export_ProducerPromotions()
		{
			var testUser = session.Load<TestUser>(user.Id);

			var testProducerUser = new TestProducerUser()
			{
				Login = "Тестовый пользователь производителя",
				TypeUser = 0
			};

			session.Save(testProducerUser);

			var testProducerPromotion = DataMother.CreateProducerPromotion(session, testUser);
			testProducerPromotion.ProducerUserId = testProducerUser.Id;
			session.Save(testProducerPromotion);

			exporter.ExportProducerPromotions();

			var files = ListResult();

			string filesSuccess = "ProducerPromotions.meta.txt,";			filesSuccess += " ProducerPromotions.txt,";
			filesSuccess += " ProducerPromotionCatalogs.meta.txt,";			filesSuccess += " ProducerPromotionCatalogs.txt,";
			filesSuccess += " ProducerPromotionSuppliers.meta.txt,";			filesSuccess += " ProducerPromotionSuppliers.txt";

			Assert.AreEqual(filesSuccess, files);

			session.Delete(testProducerPromotion);
			session.Delete(testProducerUser);
		}

		[Test]
		public void Do_not_export_waybill_files()
		{
			user.SendWaybills = false;
			CreateWaybillWithFile();
			exporter.ExportDocs();
			var files = ListResult();
			Assert.AreEqual("Waybills.meta.txt, Waybills.txt," +
				" WaybillLines.meta.txt, WaybillLines.txt," +
				" WaybillOrders.meta.txt, WaybillOrders.txt," +
				" OrderRejects.meta.txt, OrderRejects.txt, OrderRejectLines.meta.txt, OrderRejectLines.txt," +
				" LoadedDocuments.meta.txt, LoadedDocuments.txt",
				files);
		}

		[Test]
		public void Sync_only_changed()
		{
			InitAd();
			var result = ReadResult();
			var zeros = new [] {
				"catalogs.txt", "catalognames.txt", "offers.txt", "rejects.txt", "RegulatorRegistry.txt"
			};
			foreach (var zero in zeros) {
				var entry = result.FirstOrDefault(r => r.FileName.Match(zero));
				Assert.IsNotNull(entry, "Не удалось найти {0}", zero);
				Assert.That(entry.UncompressedSize, Is.GreaterThan(0),
					"пользователь {0} файл {1}", user.Id, zero);
			}

			var resultFiles = result.Implode(r => r.FileName);
			Assert.That(resultFiles, Does.Contain("MaxProducerCosts"));

			exporter.Confirm(new ConfirmRequest(requestLog.Id));

			//кеш данной сессии неактуальный тк обновление происходит в другой сессии
			var data = session.Load<AnalitfNetData>(user.Id);
			Init(data.LastUpdateAt);
			result = ReadResult();
			foreach (var zero in zeros) {
				var entry = result.FirstOrDefault(r => r.FileName.Match(zero));
				//если объект не нашли значит мы не экспортируем его и все как и должно быть
				if (entry != null)
					Assert.AreEqual(0, entry.UncompressedSize, "пользователь {0} файл {1}", user.Id, zero);
			}
			resultFiles = result.Implode(r => r.FileName);
			Assert.That(resultFiles, Is.Not.StringContaining("MaxProducerCosts"));
			Assert.That(resultFiles, Is.Not.StringContaining("ads"));
		}

		[Test]
		public void Reload_ad_on_cumulative_update()
		{
			InitAd();
			var result = ReadResult().Implode(x => x.FileName);
			Assert.That(result, Does.Contain("ads"));

			Init();
			result = ReadResult().Implode(x => x.FileName);
			Assert.That(result, Does.Contain("ads"));
		}

		[Test(Description = "Флаг синхронизации прайс-листов должен быть сброшен только при подтверждении")]
		public void Do_not_skip_unconfirmed_prices()
		{
			var result = ReadResult();
			var size = result.First(r => r.FileName.Match("offers.txt")).UncompressedSize;
			Assert.That(size, Is.GreaterThan(0));
			var data = session.Load<AnalitfNetData>(user.Id);
			data.LastUpdateAt = DateTime.Now.AddDays(-1);
			Init(data.LastUpdateAt);
			result = ReadResult();
			//10 - на случай перевода строки
			Assert.AreEqual(size, result.First(r => r.FileName.Match("offers.txt")).UncompressedSize);
		}

		[Test]
		public void Export_converted_waybill()
		{
			user.SendWaybills = true;
			var waybill = CreateWaybillWithFile();
			waybill.Log.IsFake = true;
			session.Flush();

			exporter.ExportDocs();
			var files = ListResult();
			Assert.That(files, Is.Not.StringContaining("Waybills/" + Path.GetFileName(waybill.Log.LocalFile)));
			var result = exporter.Result.First(r => r.ArchiveFileName == "Waybills.txt");
			Assert.That(new FileInfo(result.LocalFileName).Length, Is.GreaterThan(10), File.ReadAllText(result.LocalFileName));
		}

		[Test]
		public void Cost_optimization()
		{
			var supplier = TestSupplier.CreateNaked(session);
			supplier.CreateSampleCore(session);
			var rule = new CostOptimizationRule(session.Load<Supplier>(supplier.Id), RuleType.MaxCost) {
				Diapasons = { new CostOptimizationDiapason(0, decimal.MaxValue, 20) },
				Clients = { session.Load<Client>(client.Id) }
			};
			session.Save(rule);
			client.MaintainIntersection(session);
			client.Users[0].CleanPrices(session, supplier);
			session.Flush();
			exporter.ExportAll();
			var offers = ParseData("offers");
			var offerData = offers.First();
			var id = Convert.ToUInt64(offerData[0]);
			var offer = supplier.Prices[0].Core.First(c => c.Id == id);
			var resultCost = Convert.ToDecimal(GetColumnValue("Offers", "Cost", offerData), CultureInfo.InvariantCulture);
			Assert.AreEqual(offer.Costs[0].Cost * 1.2m, resultCost);
		}

		[Test]
		public void Cost_optimization_reset_fresh()
		{
			var supplier = TestSupplier.CreateNaked(session);
			supplier.CreateSampleCore(session);
			var supplier2 = TestSupplier.CreateNaked(session);
			supplier2.CreateSampleCore(session);
			var rule = new CostOptimizationRule(session.Load<Supplier>(supplier.Id), RuleType.MaxCost) {
				Diapasons = { new CostOptimizationDiapason(0, decimal.MaxValue, 20) },
				Clients = { session.Load<Client>(client.Id) },
				Concurrents = { session.Load<Supplier>(supplier2.Id) }
			};
			session.Save(rule);
			client.MaintainIntersection(session);
			client.Users[0].CleanPrices(session, supplier, supplier2);
			session.Flush();
			exporter.ExportAll();
			exporter.Confirm(new ConfirmRequest(requestLog.Id));

			supplier2.InvalidateCache(session, user.Id);

			Init(session.Load<AnalitfNetData>(user.Id).LastUpdateAt);
			exporter.ExportAll();
			var ids = ParseData("offers").Select(l => Convert.ToUInt64(l[0])).ToArray();
			Assert.IsTrue(ids.Contains(supplier.Prices[0].Core[0].Id), ids.Implode());
			var priceData = ParseData("prices").First(d => Convert.ToUInt32(d[0]) == supplier.Prices[0].Id);
			Assert.AreEqual(1, Convert.ToUInt32(priceData[17]));
		}

		[Test(Description = "Оптимизация цен должна производится только после обновления поставщиком прайс-листа, в инфом случае данные нужно кешировать")]
		public void Cache_optimized_costs()
		{
			var supplier = TestSupplier.CreateNaked(session);
			var products = TestProduct.RandomProducts(session).Take(2).ToArray();
			supplier.CreateSampleCore(session, new[] { products[0] });
			var supplier2 = TestSupplier.CreateNaked(session);
			supplier2.CreateSampleCore(session, new[] { products[1] });
			var rule = new CostOptimizationRule(session.Load<Supplier>(supplier.Id), RuleType.MaxCost) {
				Diapasons = { new CostOptimizationDiapason(0, decimal.MaxValue, 20) },
				Clients = { session.Load<Client>(client.Id) },
				Concurrents = { session.Load<Supplier>(supplier2.Id) }
			};
			session.Save(rule);
			client.Users[0].CleanPrices(session, supplier, supplier2);
			client.MaintainIntersection(session);
			session.Flush();
			exporter.ExportAll();
			exporter.Confirm(new ConfirmRequest(requestLog.Id));

			var id = supplier.Prices[0].Core[0].Id;
			var offers = ParseData("offers").ToArray();
			var offer = offers.First(x => Convert.ToUInt64(x[0]) == id);
			Assert.AreEqual(120, Convert.ToDecimal(GetColumnValue("Offers", "Cost", offer), CultureInfo.InvariantCulture));

			//симулируем обновление прайс-листа
			supplier2.CreateSampleCore(session, new[] { products[0] }, new[] { supplier.Prices[0].Core[0].Producer });
			supplier2.InvalidateCache(session, user.Id);

			Init(session.Load<AnalitfNetData>(user.Id).LastUpdateAt);
			exporter.ExportAll();
			offers = ParseData("offers").ToArray();
			offer = offers.First(x => Convert.ToUInt64(x[0]) == id);
			Assert.AreEqual(120, Convert.ToDecimal(GetColumnValue("Offers", "Cost", offer), CultureInfo.InvariantCulture));
		}

		[Test]
		public void Do_not_create_duplicate_pending_logs()
		{
			var supplier = TestSupplier.CreateNaked(session);
			var mail = new TestMail(supplier);
			session.Save(mail);
			client.CreateUser(session);
			foreach (var user in client.Users) {
				session.Save(new TestMailSendLog(user, mail));
			}
			session.CreateSQLQuery("delete from Logs.PendingMailLogs").ExecuteUpdate();
			exporter.ExportAll();
			Assert.AreEqual(1, session.Query<PendingMailLog>().Count());
		}

		private string GetColumnValue(string table, string column, string[] data)
		{
			var meta = exporter.Result
				.First(r => Path.GetFileName(r.ArchiveFileName).Match(table + ".meta.txt"))
				.ReadContent()
				.Split(new [] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
				.Where(x => x != "truncate")
				.ToArray();
			return data[meta.IndexOf(column)];
		}

		private IEnumerable<string[]> ParseData(string name)
		{
			return exporter.Result
				.First(r => Path.GetFileName(r.ArchiveFileName).Match(name + ".txt"))
				.ReadContent()
				.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(l => l.Split('\t'));
		}

		private void Init(DateTime? lastSync = null)
		{
			if (!session.Transaction.IsActive)
				session.BeginTransaction();
			if (lastSync != null) {
				//в базе даты хранятся с точностью до секунды
				lastSync = new DateTime(lastSync.Value.Year, lastSync.Value.Month, lastSync.Value.Day,
					lastSync.Value.Hour, lastSync.Value.Minute, lastSync.Value.Second);
			}
			requestLog = new RequestLog(user, Version.Parse("1.1")) {
				LastSync = lastSync
			};
			session.Save(requestLog);
			//надо очистить тк в сессии остались activeprices
			string adsPath = null;
			if (exporter != null) {
				session.Flush();
				session.Clear();
				adsPath = exporter.AdsPath;
			}
			exporter?.Dispose();
			exporter = new Exporter(session, config, requestLog) {
				Prefix = "1",
				ResultPath = "data",
			};
			if (adsPath != null)
				exporter.AdsPath = adsPath;
		}

		private void InitAd()
		{
			FileHelper.InitDir("ads");
			FileHelper.CreateDirectoryRecursive(@"ads\Воронеж_1\");
			FileHelper.Touch(@"ads\Воронеж_1\2block.gif");
			var file = new FileInfo(@"ads\Воронеж_1\2block.gif");
			file.LastWriteTime = DateTime.Now.AddSeconds(-1);
			exporter.AdsPath = "ads";
		}

		private ZipFile ReadResult()
		{
			//для подготовки данных нужна транзакция
			if (!session.Transaction.IsActive)
				session.BeginTransaction();
			exporter.ExportAll();
			var memory = new MemoryStream();
			exporter.Compress(memory);
			memory.Position = 0;
			return ZipFile.Read(memory);
		}

		private string ListResult()
		{
			var memory = new MemoryStream();
			exporter.Compress(memory);
			memory.Position = 0;
			return ZipFile.Read(memory).Implode(l => l.FileName);
		}

		private TestWaybill CreateWaybillWithFile()
		{
			var testUser = session.Load<TestUser>(user.Id);
			var waybill = DataMother.CreateWaybill(session, testUser);
			var log = waybill.Log;
			session.Save(waybill);
			var sendLog = new TestDocumentSendLog(testUser, log);
			session.Save(sendLog);
			waybill.Log.CreateFile(FixtureSetup.Config.DocsPath, "waybill content");
			return waybill;
		}

		private void ExportCompressed()
		{
			exporter.ExportAll();
			file = exporter.Compress(file);
		}
	}
}
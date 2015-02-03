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

namespace AnalitF.Net.Service.Test
{
	[TestFixture]
	public class ExporterFixture : IntegrationFixture
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
			client = TestClient.CreateNaked();
			session.Save(client);

			config = FixtureSetup.Config;
			user = session.Load<User>(client.Users[0].Id);
			FileHelper.InitDir("data", "update");
			if (!Directory.Exists(config.LocalExportPath))
				Directory.CreateDirectory(config.LocalExportPath);
			else
				Directory.GetFiles(config.LocalExportPath).Each(File.Delete);

			file = "data.zip";
			File.Delete(file);
			Init();
		}

		[TearDown]
		public void TearDown()
		{
			exporter.Dispose();
		}

		[Test]
		public void Export_update()
		{
			File.WriteAllText("update\\version.txt", "1.2");
			File.WriteAllBytes("update\\analitf.net.client.exe", new byte[] { 0x00 });

			exporter.ExportAll();
			ExportCompressed();
			var files = ZipHelper.lsZip(file);

			Assert.That(files.Implode(), Is.StringContaining("update/analitf.net.client.exe"));
		}

		[Test]
		public void Export_meta()
		{
			exporter.ExportAll();
			ExportCompressed();
			var zipEntries = ZipHelper.lsZip(file).Implode();

			Assert.That(File.Exists(file), "{0} не существует", file);
			Assert.That(zipEntries, Is.StringContaining("Addresses.txt"));
			Assert.That(zipEntries, Is.StringContaining("Addresses.meta.txt"));

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

			exporter.ExportAll();
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

			Assert.That(files, Is.StringContaining("ads/2block.gif"));
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
			client.MaintainIntersection();
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
			Assert.That(text, Is.StringContaining(core1.Id.ToString()));
			Assert.That(text, Is.StringContaining(core2.Id.ToString()));
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
				+ " LoadedDocuments.meta.txt, LoadedDocuments.txt",
				Path.GetFileName(waybillFile)));
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
				" LoadedDocuments.meta.txt, LoadedDocuments.txt",
				files);
		}

		[Test]
		public void Sync_only_changed()
		{
			InitAd();
			exporter.ExportAll();
			var result = ReadResult();
			var zeros = new [] {
				"catalogs.txt", "catalognames.txt", "offers.txt", "rejects.txt"
			};
			foreach (var zero in zeros) {
				Assert.That(result.First(r => r.FileName.Match(zero)).UncompressedSize, Is.GreaterThan(0),
					"пользователь {0} файл {1}", user.Id, zero);
			}

			var resultFiles = result.Implode(r => r.FileName);
			Assert.That(resultFiles, Is.StringContaining("MaxProducerCosts"));

			var controller = new MainController {
				CurrentUser = user,
				Session = session
			};
			controller.Delete();

			session.Flush();
			session.Clear();
			Init(session.Load<AnalitfNetData>(user.Id).LastUpdateAt);
			exporter.AdsPath = "ads";
			exporter.ExportAll();
			result = ReadResult();
			foreach (var zero in zeros) {
				Assert.AreEqual(0, result.First(r => r.FileName.Match(zero)).UncompressedSize,
					"пользователь {0} файл {1}", user.Id, zero);
			}
			resultFiles = result.Implode(r => r.FileName);
			Assert.That(resultFiles, Is.Not.StringContaining("MaxProducerCosts"));
			Assert.That(resultFiles, Is.Not.StringContaining("ads"));
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
			Assert.AreEqual(offer.Costs[0].Cost * 1.2m, Convert.ToDecimal(offerData[31], CultureInfo.InvariantCulture));
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
			var controller = new MainController {
				CurrentUser = user,
				Session = session
			};
			controller.Delete();
			session.CreateSQLQuery(@"update Usersettings.AnalitFReplicationInfo
set ForceReplication = 1
where userId = :userId and FirmCode = :supplierId")
				.SetParameter("supplierId", supplier2.Id)
				.SetParameter("userId", user.Id)
				.ExecuteUpdate();

			session.Flush();
			session.Clear();
			Init(session.Load<AnalitfNetData>(user.Id).LastUpdateAt);
			exporter.ExportAll();
			var ids = ParseData("offers").Select(l => Convert.ToUInt64(l[0])).ToArray();
			Assert.IsTrue(ids.Contains(supplier.Prices[0].Core[0].Id), ids.Implode());
			var priceData = ParseData("prices").First(d => Convert.ToUInt32(d[0]) == supplier.Prices[0].Id);
			Assert.AreEqual(1, Convert.ToUInt32(priceData[17]));
		}

		private IEnumerable<string[]> ParseData(string name)
		{
			return exporter.Result
				.First(r => Path.GetFileNameWithoutExtension(r.ArchiveFileName).Match(name))
				.ReadContent()
				.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(l => l.Split('\t'));
		}

		private void Init(DateTime? lastSync = null)
		{
			requestLog = new RequestLog(user, Version.Parse("1.1")) {
				LastSync = lastSync
			};
			exporter = new Exporter(session, config, requestLog) {
				Prefix = "1",
				ResultPath = "data",
				UpdatePath = "update",
			};
		}

		private void InitAd()
		{
			FileHelper.InitDir("ads");
			FileHelper.CreateDirectoryRecursive(@"ads\Воронеж_1\");
			File.WriteAllBytes(@"ads\Воронеж_1\2block.gif", new byte[] { 0x00 });
			var file = new FileInfo(@"ads\Воронеж_1\2block.gif");
			file.LastWriteTime = DateTime.Now.AddSeconds(-1);
			exporter.AdsPath = "ads";
		}

		private ZipFile ReadResult()
		{
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
			file = exporter.Compress(file);
		}
	}
}
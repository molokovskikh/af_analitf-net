﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AnalitF.Net.Service.Models;
using AnalitF.Net.Service.Test.TestHelpers;
using Common.Models;
using Common.Tools;
using Ionic.Zip;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Data;
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

		[SetUp]
		public void Setup()
		{
			client = TestClient.CreateNaked();
			session.Save(client);

			var config = FixtureSetup.Config;
			user = session.Load<User>(client.Users[0].Id);
			FileHelper.InitDir(config.LocalExportPath, "data", "update");

			file = "data.zip";
			File.Delete(file);
			exporter = new Exporter(session, config, new RequestLog(user, Version.Parse("1.1"))) {
				Prefix = "1",
				ResultPath = "data",
				UpdatePath = "update",
			};
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

			ExportCompressed();
			var files = ZipHelper.lsZip(file);

			Assert.That(files.Implode(), Is.StringContaining("update/analitf.net.client.exe"));
		}

		[Test]
		public void Export_meta()
		{
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

			ExportCompressed();
			var files = ZipHelper.lsZip(file);
			Assert.IsTrue(files.Any(f => Regex.IsMatch(f, @"newses/\d+.html")), files.Implode());
		}

		[Test]
		public void Export_ads()
		{
			FileHelper.InitDir("ads");
			FileHelper.CreateDirectoryRecursive(@"ads\Воронеж_1\");
			exporter.AdsPath = "ads";
			File.WriteAllBytes(@"ads\Воронеж_1\2block.gif", new byte[] { 0x00 } );

			ExportCompressed();
			var zipEntries = ZipHelper.lsZip(file);

			Assert.That(zipEntries.Implode(), Is.StringContaining("ads/2block.gif"));
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

			var files = Export();

			var offers = files.First(t => t.ArchiveFileName.EndsWith("offers.txt"));
			var text = File.ReadAllText(offers.LocalFileName);
			Assert.That(text, Is.StringContaining(core1.Id.ToString()));
			Assert.That(text, Is.StringContaining(core2.Id.ToString()));
		}

		[Test]
		public void Export_waybills()
		{
			var testUser = session.Load<TestUser>(user.Id);
			var waybill = DataMother.CreateWaybill(session, testUser);
			var log = waybill.Log;
			session.Save(waybill);
			var sendLog = new TestDocumentSendLog(testUser, log);
			session.Save(sendLog);
			var waybillFile = waybill.Log.CreateFile(FixtureSetup.Config.DocsPath, "waybill content");

			exporter.UpdateType = "Waybills";
			ExportCompressed();
			var files = ZipHelper.lsZip(file).Implode();
			Assert.AreEqual(files, String.Format("Waybills/{0}, Waybills.meta.txt, Waybills.txt,"
				+ " WaybillLines.meta.txt, WaybillLines.txt, LoadedDocuments.meta.txt, LoadedDocuments.txt",
				Path.GetFileName(waybillFile)));
		}

		private List<UpdateData> Export()
		{
			var files = new List<UpdateData>();
			exporter.Export(files);
			return files;
		}

		private void ExportCompressed()
		{
			file = exporter.ExportCompressed(file);
		}
	}
}
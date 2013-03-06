using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnalitF.Net.Models;
using Common.Models;
using Common.Tools;
using Ionic.Zip;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace AnalitF.Net.Service.Test
{
	[TestFixture]
	public class ExporterFixture : IntegrationFixture
	{
		private ISession localSession;
		private User user;
		private Exporter exporter;
		private string file;
		private TestClient client;

		[SetUp]
		public void Setup()
		{
			client = TestClient.CreateNaked();
			session.Save(client);
			session.Flush();
			session.Transaction.Commit();

			localSession = FixtureSetup.Factory.OpenSession();
			localSession.BeginTransaction();

			user = localSession.Load<User>(client.Users[0].Id);
			FileHelper.InitDir("export", "data", "update");

			file = "data.zip";
			File.Delete(file);
			exporter = new Exporter(localSession, user.Id, Version.Parse("1.1")) {
				Prefix = "1",
				ExportPath = "export",
				ResultPath = "data",
				UpdatePath = "update"
			};
		}

		[TearDown]
		public void TearDown()
		{
			localSession.Dispose();
			exporter.Dispose();
		}

		[Test]
		public void Export_update()
		{
			File.WriteAllText("update\\version.txt", "1.2");
			File.WriteAllBytes("update\\analitf.net.client.exe", new byte[0]);

			file = exporter.ExportCompressed(file);
			var files = lsZip();
			Assert.That(files.Implode(), Is.StringContaining("update/analitf.net.client.exe"));
		}

		[Test]
		public void Export_meta()
		{
			file = exporter.ExportCompressed(file);

			var zipEntries = lsZip();
			var zipEntry = zipEntries[0];
			var meta = zipEntries[1];

			Assert.That(File.Exists(file), "{0} не существует", file);
			Assert.That(zipEntry, Is.EqualTo("Addresses.txt"));
			Assert.That(meta, Is.EqualTo("Addresses.meta.txt"));

			Assert.That(Directory.GetFiles("export")[0], Is.EqualTo("export\\1Addresses.txt"));
			Assert.That(Directory.GetFiles("data")[0], Is.EquivalentTo("data\\data.zip"));
			exporter.Dispose();
			Assert.That(Directory.GetFiles("export"), Is.Empty);
		}

		[Test]
		public void Export_ads()
		{
			FileHelper.InitDir("ads");
			FileHelper.CreateDirectoryRecursive(@"ads\Воронеж_1\");
			exporter.AdsPath = "ads";
			File.WriteAllBytes(@"ads\Воронеж_1\2block.gif", new byte[0]);

			file = exporter.ExportCompressed(file);

			var zipEntries = lsZip();
			Assert.That(zipEntries.Implode(), Is.StringContaining("ads/2block.gif"));
		}

		[Test]
		public void Export_all_offers()
		{
			var supplier = TestSupplier.CreateNaked();
			client.MaintainIntersection();
			var price = supplier.Prices[0];
			var product = session.Query<TestProduct>().First(p => !p.CatalogProduct.Hidden);
			var synonym = price.AddProductSynonym(product.CatalogProduct.Name, product);
			var core1 = new TestCore(synonym);
			var core2 = new TestCore(synonym);
			session.Save(core1);
			session.Save(core2);
			core1.AddCost(100);
			core2.AddCost(150);
			session.Flush();
			session.Save(core1);
			session.Save(core2);
			session.Flush();
			session.Transaction.Commit();

			var files = exporter.Export();
			var offers = files.First(t => t.Item1.EndsWith("offers.txt"));
			var text = File.ReadAllText(offers.Item1);
			Assert.That(text, Is.StringContaining(core1.Id.ToString()));
			Assert.That(text, Is.StringContaining(core2.Id.ToString()));
		}

		private List<string> lsZip()
		{
			using(var zip = ZipFile.Read(file)) {
				return zip.Select(z => z.FileName).ToList();
			}
		}
	}
}
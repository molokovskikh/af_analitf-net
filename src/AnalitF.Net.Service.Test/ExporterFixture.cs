using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnalitF.Net.Models;
using Common.Models;
using Common.Tools;
using Ionic.Zip;
using NHibernate;
using NUnit.Framework;
using Test.Support;

namespace AnalitF.Net.Service.Test
{
	[TestFixture]
	public class ExporterFixture : IntegrationFixture
	{
		private ISession localSession;
		private User user;
		private Exporter exporter;
		private string file;

		[SetUp]
		public void Setup()
		{
			var client = TestClient.CreateNaked();
			session.Save(client);
			session.Flush();
			session.Transaction.Commit();

			localSession = FixtureSetup.Factory.OpenSession();
			localSession.BeginTransaction();

			user = localSession.Load<User>(client.Users[0].Id);
			FileHelper.InitDir("export", "data", "update");

			file = "data.zip";
			File.Delete(file);
			exporter = new Exporter(session, user.Id, Version.Parse("1.1")) {
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

		private List<string> lsZip()
		{
			using(var zip = ZipFile.Read(file)) {
				return zip.Select(z => z.FileName).ToList();
			}
		}
	}
}
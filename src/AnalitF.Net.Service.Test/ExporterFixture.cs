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
			FileHelper.InitDir("export", "data");
		}

		[TearDown]
		public void TearDown()
		{
			localSession.Dispose();
		}

		[Test]
		public void Export_meta()
		{
			var file = "data.zip";
			File.Delete(file);

			var exporter = new Exporter(session, user.Id) {
				Prefix = "1",
				ExportPath = "export",
				ResultPath = "data"
			};
			file = exporter.ExportCompressed(file);

			var zip = ZipFile.Read(file);
			var zipEntries = zip.ToList();
			var zipEntry = zipEntries[0];
			var meta = zipEntries[1];

			Assert.That(File.Exists(file), "{0} не существует", file);
			Assert.That(zipEntry.FileName, Is.EqualTo("Addresses.txt"));
			Assert.That(meta.FileName, Is.EqualTo("Addresses.meta.txt"));

			Assert.That(Directory.GetFiles("export")[0], Is.EqualTo("export\\1Addresses.txt"));
			Assert.That(Directory.GetFiles("data")[0], Is.EquivalentTo("data\\data.zip"));
			exporter.Dispose();
			Assert.That(Directory.GetFiles("export"), Is.Empty);
		}
	}
}
using System.IO;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.Tools;
using Devart.Data.MySql;
using System.Linq;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Models
{
	[TestFixture]
	public class SanityCheckFixture : DbCommandFixture
	{
		[TearDown]
		public void TearDown()
		{
			SetupFixture.RestoreData(session);
		}

		[Test]
		public void Make_check()
		{
			var check = new SanityCheck("data");
			check.Check();
		}

		[Test]
		public void Update_schema()
		{
			var columns = typeof(PriceTagSettings).GetProperties().Select(p => "drop column PriceTag" + p.Name).Implode();
			try {
				session.CreateSQLQuery("alter table Settings " + columns).ExecuteUpdate();
			}
			catch {
			}
			var check = new SanityCheck("data");
			check.Check();

			var settings = session.Query<Settings>().First();
			Assert.IsTrue(settings.PriceTag.PrintProduct);
		}

		[Test, Ignore("Тест не работает тк нельзя удалить директорию с данными тк в ней сидит mysql а способа остановить mysql нет")]
		public void Create_local_db()
		{
			MySqlConnection.ClearAllPools(true);
			var dataPath = "data";
			if (Directory.Exists(dataPath))
				Directory.GetFiles("data").Each(f => File.Delete(f));

			var sanityCheck = new SanityCheck(dataPath);
			sanityCheck.Check();

			Assert.That(Directory.Exists(dataPath));
		}
	}
}
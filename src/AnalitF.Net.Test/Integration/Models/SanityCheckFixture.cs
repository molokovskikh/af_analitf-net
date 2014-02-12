using System.IO;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.Tools;
using Devart.Data.MySql;
using System.Linq;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.Models
{
	[TestFixture]
	public class SanityCheckFixture : DbFixture
	{
		private SanityCheck check;

		[TearDown]
		public void TearDown()
		{
			restore = true;
			check = new SanityCheck();
			check.Config = config;
		}

		[Test]
		public void Make_check()
		{
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
			check.Check();

			var settings = session.Query<Settings>().First();
			Assert.IsTrue(settings.PriceTag.PrintProduct);
		}

		[Test]
		public void Create_local_db()
		{
			MySqlConnection.ClearAllPools(true);
			if (Directory.Exists(config.DbDir))
				Directory.GetFiles(config.DbDir).Each(f => File.Delete(f));

			check.Check();

			Assert.That(Directory.Exists(config.DbDir));
		}
	}
}
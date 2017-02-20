using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.Tools;
using Devart.Data.MySql;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.Models
{
	[TestFixture]
	public class SanityCheckFixture : DbFixture
	{
		private SanityCheck check;

		[SetUp]
		public void Setup()
		{
			restore = true;
			check = InitCmd(new SanityCheck());
		}

		[Test]
		public void Make_check()
		{
			check.Check();
		}

		[Test]
		public void Update_scheam_fix_error()
		{
			session.CreateSQLQuery("alter table Offers add fulltext (ProductSynonym);").ExecuteUpdate();
			check.UpgradeSchema();
			var create = session.CreateSQLQuery("show create table Offers;")
				.UniqueResult<object[]>()[1].ToString();
			Assert.That(create, Is.Not.StringContaining("ProductSynonym_2"), create);
		}

		[Test]
		public void Update_scheam_fix_error_PriceTag()
		{
			session.CreateSQLQuery("delete from PriceTagItems;").ExecuteUpdate();
			session.CreateSQLQuery("delete from PriceTags;").ExecuteUpdate();
			session.CreateSQLQuery("insert into PriceTags () values ();").ExecuteUpdate();
			session.CreateSQLQuery("insert into PriceTags () values ();").ExecuteUpdate(); // должен быть удалён
			session.CreateSQLQuery("insert into PriceTagItems () values ();").ExecuteUpdate(); // должен быть привязан
			check.UpgradeSchema();
			var tag = session.Query<PriceTag>().Single();
			Assert.AreEqual(tag.Items.Count, 1);
		}

		[Test]
		public void Update_column_types()
		{
			session.CreateSQLQuery("alter table Settings change column ProxyPort ProxyPort int NOT NULL DEFAULT '0';")
				.ExecuteUpdate();
			check.Check(true);
			var result = session.CreateSQLQuery("show create table Settings")
				.UniqueResult<object[]>();
			Assert.That(result[1].ToString(), Does.Contain("`ProxyPort` int(11) DEFAULT NULL,"));

			// #51646 Проблемы обновления АF.NET
			var indexcntobj = session.CreateSQLQuery("select count(1) from information_schema.statistics " +
			"where table_name = 'WaybillLines' " +
				"and index_name = 'SerialProductIdProducerId'").UniqueResult();
			int indexcnt = indexcntobj != null ? Int32.Parse(indexcntobj.ToString()) : -1;
			Assert.AreEqual(indexcnt, 3);
		}

		[Test]
		public void Update_schema()
		{
			try {
				session.CreateSQLQuery("alter table pricetagsettings drop column PrintProduct").ExecuteUpdate();
			}
			catch {
			}
			check.Check();

			var settings = session.Query<Settings>().First();
			Assert.IsTrue(settings.PriceTags.FirstOrDefault().PrintProduct);
		}

		[Test]
		public void Create_local_db()
		{
			session.CreateSQLQuery("flush tables").ExecuteUpdate();
			session.Transaction.Commit();
			session.Disconnect();
			MySqlConnection.ClearAllPools(true);
			if (Directory.Exists(config.DbDir))
				Directory.GetFiles(config.DbDir).Each(f => FileHelper.DeleteFile(f));
			session.Reconnect();
			session.Transaction.Begin();

			check.Check();
			var result = session.CreateSQLQuery("show create table Offers").UniqueResult<object[]>();
			Assert.That(result[1].ToString(), Does.Contain("FULLTEXT KEY"));
			Assert.That(Directory.Exists(config.DbDir));
		}

		[Test]
		public void Migrate_markup_settings()
		{
			session.CreateSQLQuery(@"delete from MarkupConfigs where Type = 2").ExecuteUpdate();
			check.Check(updateSchema: true);
			var settings = session.Query<Settings>().First();
			Assert.AreEqual(1, settings.Markups.Count(x => x.Type == MarkupType.Nds18));
		}

		[Test]
		public void Sanity_check_invalid_data()
		{
			var id = address.Id + new Random().Next();
			session.CreateSQLQuery("delete from Addresses").ExecuteUpdate();
			session.CreateSQLQuery("insert into Addresses(Id) values(:id)").SetParameter("id", id).ExecuteUpdate();
			check.Check();
		}

		[Test]
		public void Create_uniq_index()
		{
			session.CreateSQLQuery("alter table Stocks drop index ServerId").ExecuteUpdate();
			check.Check(updateSchema: true);
			var result = session.CreateSQLQuery("show create table Stocks").UniqueResult<object[]>();
			Assert.That(result[1].ToString(), Does.Contain("UNIQUE KEY `ServerIdUniq` (`ServerId`)"));
		}
	}
}
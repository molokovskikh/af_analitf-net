﻿using System;
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
		public void Update_column_types()
		{
			session.CreateSQLQuery("alter table Settings change column ProxyPort ProxyPort int NOT NULL DEFAULT '0';")
				.ExecuteUpdate();
			check.Check(true);
			var result = session.CreateSQLQuery("show create table Settings")
				.UniqueResult<object[]>();
			Assert.That(result[1].ToString(), Is.StringContaining("`ProxyPort` int(11) DEFAULT NULL,"));
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
			var result = session.CreateSQLQuery("show create table Offers").UniqueResult<object[]>();
			Assert.That(result[1].ToString(), Is.StringContaining("FULLTEXT KEY"));
			Assert.That(Directory.Exists(config.DbDir));
		}
	}
}
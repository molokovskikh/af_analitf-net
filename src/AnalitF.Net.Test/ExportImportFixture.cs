using System;
using System.Collections.Generic;
using System.IO;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace AnalitF.Net.Test
{
	[TestFixture]
	public class ExportImportFixture : IntegrationFixture
	{
		private ISession localSession = null;

		[SetUp]
		public void Setup()
		{
			new Client.Config.Initializers.NHibernate().Init();
			localSession = Client.Config.Initializers.NHibernate.Factory.OpenSession();
			var export = new SchemaExport(Client.Config.Initializers.NHibernate.Configuration);
			export.Drop(true, true);
			export.Create(true, true);
		}

		[Test]
		public void Load_data()
		{
			TestSupplier.Create();
			var client = TestClient.CreateNaked();
			Close();

			var files = Export(client.Users[0].Id);
			Import(files);
		}

		private void Import(params string[] tables)
		{
			foreach (var table in tables) {
				var sql = String.Format("LOAD DATA INFILE '{0}' INTO TABLE {1}", table, Path.GetFileNameWithoutExtension(table));
				var dbCommand = session.Connection.CreateCommand();
				dbCommand.CommandText = sql;
				dbCommand.ExecuteNonQuery();
			}
		}

		private string[] Export(uint userId)
		{
			session.CreateSQLQuery("call Customers.GetActivePrices(:userId)")
				.SetParameter("userId", userId)
				.ExecuteUpdate();

			var result = new List<string>();
			var sql = @"select
ap.PriceCode as Id,
ap.PriceName as Name,
r.RegionCode as RegionId,
r.Region as RegionName,
s.Id as SupplierId,
s.Name as SupplierName,
s.FullName as SupplierFullName,
rd.Storage,
ap.PositionCount,
ap.PriceDate,
rd.OperativeInfo,
rd.ContactInfo,
rd.SupportPhone as Phone,
rd.AdminMail as Email,
ap.MinReq
from Usersettings.ActivePrices ap
	join Usersettings.PricesData pd on pd.PriceCode = ap.PriceCode
		join Customers.Suppliers s on s.Id = pd.FirmCode
	join Farm.Regions r on r.RegionCode = ap.RegionCode
	join Usersettings.RegionalData rd on rd.FirmCode = s.Id and rd.RegionCode = r.RegionCode
";

			result.Add(Export(sql, "prices"));

			sql = @"
";
			result.Add(Export(sql, "offers"));

			return result.ToArray();
		}

		public string Export(string sql, string file)
		{
			var exportFile = Path.GetFullPath(file + ".txt").Replace(@"\", "/");
			File.Delete(exportFile);
			sql += " INTO OUTFILE '" + exportFile + "' ";
			session.CreateSQLQuery(sql).ExecuteUpdate();
			return exportFile;
		}
	}
}
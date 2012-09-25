using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Common.Models.Repositories;
using Common.Tools;
using MySql.Data.MySqlClient;
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
			export.Drop(false, true);
			export.Create(false, true);
		}

		[Test]
		public void Load_data()
		{
			var supplier = TestSupplier.Create();
			supplier.CreateSampleCore();
			var client = TestClient.CreateNaked();
			Close();

			var files = Export(client.Users[0].Id);
			Import(files);
		}

		private void Import(List<Tuple<string, string[]>> tables)
		{
			foreach (var table in tables) {
				var sql = String.Format("LOAD DATA INFILE '{0}' INTO TABLE {1} ({2})",
					table.Item1,
					Path.GetFileNameWithoutExtension(table.Item1),
					table.Item2.Implode());
				var dbCommand = session.Connection.CreateCommand();
				dbCommand.CommandText = sql;
				dbCommand.ExecuteNonQuery();
			}
		}

		private List<Tuple<string, string[]>> Export(uint userId)
		{
			session.CreateSQLQuery("call Customers.GetOffers(:userId)")
				.SetParameter("userId", userId)
				.ExecuteUpdate();

			var result = new List<Tuple<string, string[]>>();
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

			var offerQuery = new OfferQuery();
			offerQuery
				.Select("m.PriceCode as LeaderPriceId", "m.MinCost as LeaderCost", "lr.Region as LeaderRegion", "p.CatalogId")
				.Join("join Usersettings.MinCosts m on m.Id = c0.Id and m.RegionCode = ap.RegionCode")
				.Join("join Farm.Regions lr on lr.RegionCode = m.RegionCode")
				.Join("join Catalogs.Products p on p.Id = c0.ProductId");
			offerQuery.SelectSynonyms();
			sql = offerQuery.ToSql()
				.Replace("{Offer.", "")
				.Replace("}", "")
				.Replace("as Id.CoreId,", "as Id,")
				.Replace("as Id.RegionCode ,", "as RegionId,")
				.Replace("as CodeFirmCr,", "as ProducerId,")
				.Replace("as SynonymCode,", "as ProductSynonymId,")
				.Replace("as SynonymFirmCrCode,", "as ProducerSynonymId,")
				.Replace("c0.Await as Await,", "")
				.Replace("c0.UpdateTime as CoreUpdateTime,", "")
				.Replace("c0.QuantityUpdate as CoreQuantityUpdate,", "")
				.Replace("as PriceCode,", "as PriceId,");
			result.Add(Export(sql, "offers"));

			sql = @"
select
	Id,
	Name,
	EnglishName,
	Description,
	Interaction,
	SideEffect,
	IndicationsForUse,
	Dosing,
	Warnings,
	ProductForm,
	PharmacologicalAction,
	Storage,
	Expiration,
	Composition
from Catalogs.Descriptions";
			result.Add(Export(sql, "ProductDescriptions"));

			sql = @"
select cn.Id,
	cn.Name,
	cn.DescriptionId
from Catalogs.CatalogNames cn
	join Catalogs.Catalog c on c.NameId = cn.Id
where c.Hidden = 0
group by cn.Id";
			result.Add(Export(sql, "catalognames"));

			sql = @"
select cf.Id,
	cf.Form
from Catalogs.CatalogForms cf
	join Catalogs.Catalog c on c.FormId = cf.Id
where c.Hidden = 0
group by cf.Id";
			result.Add(Export(sql, "catalogforms"));

			sql = @"
select
	c.Id,
	c.NameId,
	c.FormId,
	c.VitallyImportant,
	c.MandatoryList,
	exists(select * from usersettings.Core cr join Catalogs.Products p on p.Id = cr.ProductId where p.CatalogId = c.Id) as HaveOffers
from Catalogs.Catalog c
where Hidden = 0";
			result.Add(Export(sql, "catalogs"));

			return result;
		}

		public Tuple<string, string[]> Export(string sql, string file)
		{
			var dataAdapter = new MySqlDataAdapter(sql + " limit 0", (MySqlConnection)session.Connection);
			var table = new DataTable();
			dataAdapter.Fill(table);
			var columns = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();

			var exportFile = Path.GetFullPath(file + ".txt").Replace(@"\", "/");
			File.Delete(exportFile);
			sql += " INTO OUTFILE '" + exportFile + "' ";
			session.CreateSQLQuery(sql).ExecuteUpdate();
			return Tuple.Create(exportFile, columns);
		}
	}
}
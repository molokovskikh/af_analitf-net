using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Common.Models.Repositories;
using Common.Tools;
using Ionic.Zip;
using MySql.Data.MySqlClient;
using NHibernate;

namespace AnalitF.Net.Models
{
	public class Exporter : IDisposable
	{
		private ISession session;

		private FileCleaner cleaner = new FileCleaner();
		private bool disposed;

		private uint userId;
		private Version version;

		public string Prefix = "";
		public string ExportPath = "";
		public string ResultPath = "";
		public string UpdatePath;

		public uint MaxProducerCostPriceId;
		public uint MaxProducerCostCostId;


		public Exporter(ISession session, uint userId, Version version)
		{
			this.session = session;
			this.userId = userId;
			this.version = version;
		}

		public List<Tuple<string, string[]>> Export()
		{
			var result = new List<Tuple<string, string[]>>();

			session.CreateSQLQuery("call Customers.GetOffers(:userId)")
				.SetParameter("userId", userId)
				.ExecuteUpdate();
			string sql;

			sql = @"
select a.Id,
a.Address as Name
from Customers.Addresses a
join Customers.UserAddresses ua on ua.AddressId = a.Id
where a.Enabled = 1 and ua.UserId = ?userId";
			result.Add(Export(sql, "Addresses", new { userId }));

			sql = @"
drop temporary table if exists Usersettings.MaxProducerCosts;
create temporary table Usersettings.MaxProducerCosts(
	Id bigint unsigned not null,
	ProductId int unsigned not null,
	CatalogId int unsigned not null,
	ProducerId int unsigned,
	Producer varchar(255),
	Product varchar(255),
	Cost decimal not null,
	primary key(id),
	key(ProductId, ProducerId)
) engine=memory;

insert into Usersettings.MaxProducerCosts(Id, ProductId, CatalogId, ProducerId, Cost, Product, Producer)
select c0.Id, c0.ProductId, c.Id, c0.CodeFirmCr, cc.Cost, s.Synonym, sfc.Synonym
from Farm.Core0 c0
	join Farm.CoreCosts cc on cc.Core_Id = c0.Id
	join Catalogs.Products p on p.Id = c0.ProductId
		join Catalogs.Catalog c on c.Id = p.CatalogId
	join Farm.Synonym s on s.SynonymCode = c0.SynonymCode
	left join Farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = c0.SynonymFirmCrCode
where c0.PriceCode = :priceId and cc.PC_CostCode = :costId;";
			session.CreateSQLQuery(sql)
				.SetParameter("priceId", MaxProducerCostPriceId)
				.SetParameter("costId", MaxProducerCostCostId)
				.ExecuteUpdate();

			sql = @"select * from Usersettings.MaxProducerCosts";
			result.Add(Export(sql, "MaxProducerCosts"));

			sql = @"select
ap.PriceCode as Id,
ap.PriceName as PriceName,
s.Name as Name,
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
ap.MinReq,
ap.FirmCategory as Category
from Usersettings.ActivePrices ap
	join Usersettings.PricesData pd on pd.PriceCode = ap.PriceCode
		join Customers.Suppliers s on s.Id = pd.FirmCode
	join Farm.Regions r on r.RegionCode = ap.RegionCode
	join Usersettings.RegionalData rd on rd.FirmCode = s.Id and rd.RegionCode = r.RegionCode
";

			result.Add(Export(sql, "prices"));

			var offerQuery = new OfferQuery();
			offerQuery.Select("m.PriceCode as LeaderPriceId",
				"r.Region as RegionName",
				"m.MinCost as LeaderCost",
				"lr.RegionCode as LeaderRegionId",
				"lr.Region as LeaderRegion",
				"p.CatalogId",
				"pr.Name as Producer",
				"mx.Cost as MaxProducerCost")
				.Join("join Usersettings.MinCosts m on m.Id = c0.Id and m.RegionCode = ap.RegionCode")
				.Join("join Farm.Regions lr on lr.RegionCode = m.RegionCode")
				.Join("join Catalogs.Products p on p.Id = c0.ProductId")
				.Join("join Catalogs.Catalog cl on cl.Id = p.CatalogID")
				.Join("left join Catalogs.Producers pr on pr.Id = c0.CodeFirmCr")
				.Join("join Farm.Regions r on r.RegionCode = ap.RegionCode")
				.Join("left join Usersettings.MaxProducerCosts mx on mx.ProductId = c0.ProductId and mx.ProducerId = c0.CodeFirmCr");
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
				.Replace("as PriceCode,", "as PriceId,")
				.Replace("as OrderCost,", "as MinOrderSum,")
				.Replace("c0.VitallyImportant as VitallyImportant", "c0.VitallyImportant or cl.VitallyImportant as VitallyImportant");
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
select Id,
	RussianMnn as Name,
	exists(select *
		from usersettings.Core cr
			join Catalogs.Products p on p.Id = cr.ProductId
				join Catalogs.Catalog c on c.Id = p.CatalogId
					join Catalogs.CatalogNames cn on cn.Id = c.NameId
		where m.Id = cn.MnnId) as HaveOffers
from Catalogs.Mnn m";

			result.Add(Export(sql, "mnns"));

			sql = @"
select cn.Id,
	cn.Name,
	cn.DescriptionId,
	cn.MnnId,
	exists(select *
		from usersettings.Core cr
			join Catalogs.Products p on p.Id = cr.ProductId
				join Catalogs.Catalog c on c.Id = p.CatalogId
		where c.NameId = cn.Id) as HaveOffers,
	exists(select * from Catalogs.Catalog cat where cat.NameId = cn.Id and cat.Hidden = 0 and cat.VitallyImportant = 1) as VitallyImportant,
	exists(select * from Catalogs.Catalog cat where cat.NameId = cn.Id and cat.Hidden = 0 and cat.MandatoryList = 1) as MandatoryList
from Catalogs.CatalogNames cn
where exists(select * from Catalogs.Catalog cat where cat.NameId = cn.Id and cat.Hidden = 0)
group by cn.Id";
			result.Add(Export(sql, "catalognames"));

			sql = @"
select
	c.Id,
	c.NameId,
	c.VitallyImportant,
	c.MandatoryList,
	exists(select * from usersettings.Core cr join Catalogs.Products p on p.Id = cr.ProductId where p.CatalogId = c.Id) as HaveOffers,
	cf.Id as FormId,
	cf.Form as Form
from Catalogs.Catalog c
	join Catalogs.CatalogForms cf on cf.Id = c.FormId
where Hidden = 0";
			result.Add(Export(sql, "catalogs"));

			return result;
		}

		public Tuple<string, string[]> Export(string sql, string file, object parameters = null)
		{
			var dataAdapter = new MySqlDataAdapter(sql + " limit 0", (MySqlConnection)session.Connection);
			if (parameters != null)
				ObjectExtentions.ToDictionary(parameters).Each(k => dataAdapter.SelectCommand.Parameters.AddWithValue(k.Key, k.Value));

			var table = new DataTable();
			dataAdapter.Fill(table);
			var columns = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();

			var path = Path.GetFullPath(Path.Combine(ExportPath, Prefix + file + ".txt"));
			var mysqlPath = path.Replace(@"\", "/");
			File.Delete(mysqlPath);
			sql += " INTO OUTFILE '" + mysqlPath + "' ";
			var command = new MySqlCommand(sql, (MySqlConnection)session.Connection);
			if (parameters != null)
				ObjectExtentions.ToDictionary(parameters).Each(k => command.Parameters.AddWithValue(k.Key, k.Value));
			command.ExecuteNonQuery();

			cleaner.Watch(path);

			return Tuple.Create(mysqlPath, columns);
		}

		public string ExportCompressed(string file)
		{
			var files = Export();
			using (var zip = new ZipFile()) {
				foreach (var tuple in files) {
					var entry = zip.AddFile(tuple.Item1);
					var dataname = Path.GetFileName(tuple.Item1);
					dataname = dataname.Replace(Prefix, "");
					var metaname = Path.ChangeExtension(dataname, ".meta.txt");
					entry.FileName = dataname;
					zip.AddEntry(metaname, tuple.Item2.Implode("\r\n"));
				}
				CheckUpdate(zip);
				file = Path.Combine(ResultPath, file);
				zip.Save(file);
			}
			return file;
		}

		private void CheckUpdate(ZipFile zip)
		{
			var file = Path.Combine(UpdatePath, "version.txt");
			if (!File.Exists(file))
				return;

			var updateVersion = Version.Parse(File.ReadAllText(file));
			if (updateVersion <= version)
				return;

			zip.AddDirectory(UpdatePath, "update");
		}

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			cleaner.Dispose();
		}
	}
}
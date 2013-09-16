using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Common.Models;
using Common.Models.Repositories;
using Common.Tools;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Linq;
using log4net;

namespace AnalitF.Net.Service.Models
{
	public class UpdateData
	{
		public string LocalFileName { get; set; }
		public string ArchiveFileName { get; set; }
		public string Content { get; set; }

		public UpdateData(string archiveFileName)
		{
			ArchiveFileName = archiveFileName;
		}
	}

	public class Exporter : IDisposable
	{
		private ILog log = LogManager.GetLogger(typeof(Exporter));

		private ISession session;

		private FileCleaner cleaner = new FileCleaner();
		private bool disposed;

		private uint userId;
		private Version version;

		public string UpdateType;

		public string Prefix = "";
		public string ExportPath = "";
		public string ResultPath = "";
		public string AdsPath = "";
		public string UpdatePath;
		public string DocsPath = "";

		public uint MaxProducerCostPriceId;
		public uint MaxProducerCostCostId;

		public Exporter(ISession session, uint userId, Version version, string updateType = null)
		{
			this.session = session;
			this.userId = userId;
			this.version = version;
			this.UpdateType = updateType;
		}

		//Все даты передаются в UTC!
		public void Export(List<UpdateData> result)
		{
			session.CreateSQLQuery("drop temporary table if exists usersettings.prices;" +
				"drop temporary table if exists usersettings.activeprices;" +
				"call Customers.GetOffers(:userId);" +
				"call Customers.GetPrices(:userId);")
				.SetParameter("userId", userId)
				.ExecuteUpdate();
			string sql;

			sql = @"
select Id,
	Product,
	ProductId,
	Producer,
	ProducerId,
	Series,
	LetterNo,
	convert_tz(LetterDate, @@session.time_zone,'+00:00') as LetterDate,
	CauseRejects
from Farm.Rejects
where CancelDate is null";
			Export(result, sql, "Rejects");

			sql = @"
select a.Id,
a.Address as Name
from Customers.Addresses a
join Customers.UserAddresses ua on ua.AddressId = a.Id
where a.Enabled = 1 and ua.UserId = ?userId";
			Export(result, sql, "Addresses", new { userId });

			sql = @"
select u.Id,
	u.InheritPricesFrom is not null as IsPriceEditDisabled,
	c.FullName as FullName #version-tag-13
from Customers.Users u
	join Customers.Clients c on c.Id = u.ClientId
where u.Id = ?userId";
			Export(result, sql, "Users", new { userId });

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

			sql = @"
select
	a.Id as AddressId,
	i.PriceId as PriceId,
	i.RegionId as RegionId,
	if(ai.MinReq > 0, ai.MinReq, p.MinReq) as MinOrderSum,
	ai.ControlMinReq as IsRuleMandatory
from
  Customers.Users u
  join Customers.Clients c on u.ClientId = c.Id
  join Customers.UserAddresses ua on ua.UserId = u.Id
  join Customers.Addresses a on c.Id = a.ClientId and ua.AddressId = a.Id
  join Customers.Intersection i on i.ClientId = c.Id
  join Customers.AddressIntersection ai on ai.IntersectionId = i.Id and ai.AddressId = a.Id
  join Usersettings.ActivePrices p on p.PriceCode = i.PriceId and p.RegionCode = i.RegionId
where u.Id = ?UserId
	and a.Enabled = 1
";
			Export(result, sql, "MinOrderSumRules", new { userId });

			sql = @"select * from Usersettings.MaxProducerCosts";
			Export(result, sql, "MaxProducerCosts");

			sql = @"select
p.PriceCode as PriceId,
p.PriceName as PriceName,
s.Name as Name,
r.RegionCode as RegionId,
r.Region as RegionName,
s.Id as SupplierId,
s.Name as SupplierName,
s.FullName as SupplierFullName,
rd.Storage,
if(p.DisabledByClient or not p.Actual, 0, p.PositionCount) as PositionCount,
convert_tz(p.PriceDate, @@session.time_zone,'+00:00') as PriceDate,
rd.OperativeInfo,
rd.ContactInfo,
rd.SupportPhone as Phone,
rd.AdminMail as Email,
p.FirmCategory as Category,
p.DisabledByClient
from Usersettings.Prices p
	join Usersettings.PricesData pd on pd.PriceCode = p.PriceCode
		join Customers.Suppliers s on s.Id = pd.FirmCode
	join Farm.Regions r on r.RegionCode = p.RegionCode
	join Usersettings.RegionalData rd on rd.FirmCode = s.Id and rd.RegionCode = r.RegionCode
";

			Export(result, sql, "prices");

			sql = @"select
s.Id,
s.Name,
s.FullName
from Customers.Suppliers s
	join Usersettings.Prices p on p.FirmCode = s.Id";
			Export(result, sql, "suppliers");

			var offerQuery = new OfferQuery();
			offerQuery.Select("m.MinCost as LeaderCost",
				"m.PriceCode as LeaderPriceId",
				"lr.RegionCode as LeaderRegionId",
				"p.CatalogId",
				"pr.Name as Producer",
				"mx.Cost as MaxProducerCost")
				.Join("join Usersettings.MinCosts m on m.ProductId = c0.ProductId and m.RegionCode = ap.RegionCode")
				.Join("join Farm.Regions lr on lr.RegionCode = m.RegionCode")
				.Join("join Catalogs.Products p on p.Id = c0.ProductId")
				.Join("join Catalogs.Catalog cl on cl.Id = p.CatalogID")
				.Join("left join Catalogs.Producers pr on pr.Id = c0.CodeFirmCr")
				.Join("join Farm.Regions r on r.RegionCode = ap.RegionCode")
				.Join("left join Usersettings.MaxProducerCosts mx on mx.ProductId = c0.ProductId and mx.ProducerId = c0.CodeFirmCr");
			offerQuery.SelectSynonyms();
			//в MaxProducerCosts может быть более одной записи
			offerQuery.GroupBy("c0.Id, ap.RegionCode");
			sql = offerQuery.ToSql()
				.Replace("{Offer.", "")
				.Replace("}", "")
				.Replace("as Id.CoreId,", "as OfferId,")
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
			Export(result, sql, "offers");

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
from Catalogs.Descriptions
where NeedCorrect = 0";
			Export(result, sql, "ProductDescriptions");

			sql = @"
select Id,
	Mnn as Name,
	exists(select *
		from usersettings.Core cr
			join Catalogs.Products p on p.Id = cr.ProductId
				join Catalogs.Catalog c on c.Id = p.CatalogId
					join Catalogs.CatalogNames cn on cn.Id = c.NameId
		where m.Id = cn.MnnId) as HaveOffers
from Catalogs.Mnn m";

			Export(result, sql, "mnns");

			//todo: когда будет накопительное обновление
			//нужно обработать обновление descriptionid когда станет NeedCorrect = 0
			sql = @"
select cn.Id,
	cn.Name,
	if(d.NeedCorrect = 1, null, cn.DescriptionId) as DescriptionId,
	cn.MnnId,
	exists(select *
		from usersettings.Core cr
			join Catalogs.Products p on p.Id = cr.ProductId
				join Catalogs.Catalog c on c.Id = p.CatalogId
		where c.NameId = cn.Id) as HaveOffers,
	exists(select * from Catalogs.Catalog cat where cat.NameId = cn.Id and cat.Hidden = 0 and cat.VitallyImportant = 1) as VitallyImportant,
	exists(select * from Catalogs.Catalog cat where cat.NameId = cn.Id and cat.Hidden = 0 and cat.MandatoryList = 1) as MandatoryList
from Catalogs.CatalogNames cn
	left join Catalogs.Descriptions d on d.Id = cn.DescriptionId
where exists(select * from Catalogs.Catalog cat where cat.NameId = cn.Id and cat.Hidden = 0)
group by cn.Id";
			Export(result, sql, "catalognames");

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
			Export(result, sql, "catalogs");

			ExportDocs(result);
		}

		private void ExportDocs(List<UpdateData> result)
		{
			string sql;

			session.CreateSQLQuery(@"delete from Logs.PendingDocLogs"
				+ " where UserId = :userId;")
				.SetParameter("userId", userId)
				.ExecuteUpdate();

			var logs = session.Query<DocumentSendLog>()
				.Where(l => !l.Committed && l.User.Id == userId)
				.Take(400)
				.ToArray();

			if (logs.Length == 0)
				return;

			foreach (var log in logs) {
				log.Committed = false;
				log.FileDelivered = false;
				log.DocumentDelivered = false;
			}
			try {
				foreach (var log in logs) {
					var type = log.Document.DocumentType.ToString();
					var path = Path.Combine(DocsPath,
						log.Document.AddressId.ToString(),
						type);
					if (!Directory.Exists(path))
						continue;
					var files = Directory.GetFiles(path, String.Format("{0}_*", log.Document.Id));
					result.AddRange(files.Select(f => new UpdateData(Path.Combine(type, Path.GetFileName(f))) {
						LocalFileName = f
					}));
					if (files.Length > 0)
						log.FileDelivered = true;
				}
			}
			catch(Exception e) {
				log.Warn("Ошибка при экспорте файлов накладных", e);
			}

			var ids = logs.Select(d => d.Document.Id).Implode();
			sql = String.Format(@"
select d.RowId as Id,
	dh.ProviderDocumentId,
	convert_tz(dh.WriteTime, @@session.time_zone,'+00:00') as WriteTime,
	convert_tz(dh.DocumentDate, @@session.time_zone,'+00:00') as DocumentDate,
	dh.AddressId,
	dh.FirmCode as SupplierId,
	i.SellerName,
	i.SellerAddress,
	i.SellerInn,
	i.SellerKpp,
	i.BuyerName,
	i.BuyerAddress,
	i.BuyerInn,
	i.BuyerKpp,
	i.ConsigneeInfo as ConsigneeNameAndAddress,
	i.ShipperInfo as ShipperNameAndAddress
from Logs.Document_logs d
	join Documents.DocumentHeaders dh on dh.DownloadId = d.RowId
		left join Documents.InvoiceHeaders i on i.Id = dh.Id
where d.RowId in ({0})
group by dh.Id", ids);
			Export(result, sql, "Waybills", new { userId });

			sql = String.Format(@"
select db.Id,
	d.RowId as WaybillId,
	db.Product,
	db.Producer,
	db.Country,
	db.ProducerCost,
	db.RegistryCost,
	db.SupplierPriceMarkup,
	db.SupplierCostWithoutNds,
	db.SupplierCost,
	db.Quantity,
	db.Nds,
	db.SerialNumber,
	db.Amount,
	db.NdsAmount,
	db.Unit,
	db.BillOfEntryNumber,
	db.ExciseTax,
	db.VitallyImportant,
	db.Period,
	db.Certificates
from Logs.Document_logs d
		join Documents.DocumentHeaders dh on dh.DownloadId = d.RowId
			join Documents.DocumentBodies db on db.DocumentId = dh.Id
where d.RowId in ({0})
group by dh.Id, db.Id", ids);
			Export(result, sql, "WaybillLines", new { userId });

			Export(result,
				"LoadedDocuments",
				new[] { "Id", "Type", "SupplierId", "OriginFilename", },
				logs.Where(l => l.FileDelivered).Select(l => new object[] {
					l.Document.Id,
					(int)l.Document.DocumentType,
					l.Document.Supplier.Id,
					l.Document.Filename,
				}));

			var documentExported = session.CreateSQLQuery(@"
select d.RowId
from Logs.Document_logs d
	join Documents.DocumentHeaders dh on dh.DownloadId = d.RowId
where d.RowId in (:ids)
group by dh.Id")
				.SetParameterList("ids", logs.Select(d => d.Document.Id).ToArray())
				.List<uint>();

			logs.Where(l => documentExported.Contains(l.Document.Id))
				.Each(l => l.DocumentDelivered = true);

			logs.Where(l => l.DocumentDelivered || l.FileDelivered)
				.Select(l => new PendingDocLog(l))
				.Each(p => session.Save(p));
		}

		public void Export(List<UpdateData> data, string name, string[] meta, IEnumerable<object[]> exportData)
		{
			var filename = Path.GetFullPath(Path.Combine(ExportPath, Prefix + name + ".txt"));
			data.Add(new UpdateData(name + ".meta.txt") { Content = meta.Implode("\r\n") });
			data.Add(new UpdateData(name + ".txt") { LocalFileName = filename });
			try {
				using(var file = new StreamWriter(File.OpenWrite(filename), Encoding.GetEncoding(1251))) {
					foreach (var item in exportData) {
						for(var i = 0; i < item.Length; i++) {
							if (item[i] == null)
								file.Write(@"\N");
							else
								file.Write(item[i]);
							file.Write("\t");
						}
					}
					file.WriteLine();
				}
			}
			finally {
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			}
		}

		public void Export(List<UpdateData> data, string sql, string file, object parameters = null)
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

			log.DebugFormat("Запрос {0}", sql);
			var watch = new Stopwatch();
			watch.Start();
			command.ExecuteNonQuery();
			watch.Stop();
			log.DebugFormat("Занял {0}с", watch.Elapsed.TotalSeconds);

			cleaner.Watch(path);

			data.Add(new UpdateData(file + ".meta.txt") { Content = columns.Implode("\r\n") });
			data.Add(new UpdateData(file + ".txt") { LocalFileName = path });
		}

		public string ExportCompressed(string file)
		{
			var files = new List<UpdateData>();
			if (UpdateType.Match("waybills")) {
				ExportDocs(files);
			}
			else {
				Export(files);
				CheckUpdate(files);
				CheckAds(files);
			}

			var watch = new Stopwatch();
			watch.Start();
			file = Path.Combine(ResultPath, file);
			using (var zip = ZipFile.Create(file)) {
				((ZipEntryFactory)zip.EntryFactory).IsUnicodeText = true;
				zip.BeginUpdate();
				foreach (var tuple in files) {
					if (String.IsNullOrEmpty(tuple.LocalFileName)) {
						var content = new MemoryDataSource(new MemoryStream(Encoding.UTF8.GetBytes(tuple.Content)));
						zip.Add(content, tuple.ArchiveFileName);
					}
					else {
						zip.Add(tuple.LocalFileName, tuple.ArchiveFileName);
					}
				}
				zip.CommitUpdate();
			}
			watch.Stop();

			log.DebugFormat("Архив создан за {0}, размер {1}", watch.Elapsed, new FileInfo(file).Length);

			return file;
		}

		public class MemoryDataSource : IStaticDataSource
		{
			private Stream data;

			public MemoryDataSource(Stream data)
			{
				this.data = data;
			}

			public Stream GetSource()
			{
				return data;
			}
		}

		public void CheckAds(List<UpdateData> zip)
		{
			var user = session.Load<User>(userId);
			if (!Directory.Exists(AdsPath))
				return;
			var template = String.Format("_{0}", user.Client.RegionCode);
			var dir = Directory.GetDirectories(AdsPath).FirstOrDefault(d => d.EndsWith(template));
			if (String.IsNullOrEmpty(dir))
				return;

			AddDir(zip, dir, "ads");
		}

		private static void AddDir(List<UpdateData> zip, string dir, string name)
		{
			var transform = new ZipNameTransform();
			transform.TrimPrefix = dir;

			var scanned = new FileSystemScanner(".+");
			scanned.ProcessFile += (sender, args) => {
				if (new FileInfo(args.Name).Attributes.HasFlag(FileAttributes.Hidden))
					return;
				if (name != "")
					name = name + "/";
				zip.Add(new UpdateData(name + transform.TransformFile(args.Name)) {
					LocalFileName = args.Name
				});
			};
			scanned.Scan(dir, true);
		}

		private void CheckUpdate(List<UpdateData> zip)
		{
			var file = Path.Combine(UpdatePath, "version.txt");
			if (!File.Exists(file))
				return;

			var updateVersion = Version.Parse(File.ReadAllText(file));
			if (updateVersion <= version)
				return;

			//в сборке 12 ошибка, обходим ее
			if (version.Revision == 12) {
				AddDir(zip, UpdatePath, "");
				AddDir(zip, UpdatePath, "update");
			}
			else {
				AddDir(zip, UpdatePath, "update");
			}
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
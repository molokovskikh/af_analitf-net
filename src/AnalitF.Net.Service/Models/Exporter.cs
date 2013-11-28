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
using Common.Models.Helpers;
using Common.Models.Repositories;
using Common.MySql;
using Common.NHibernate;
using Common.Tools;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression;
using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Linq;
using log4net;
using MySqlHelper = AnalitF.Net.Service.Helpers.MySqlHelper;

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

		public static UpdateData FromFile(string archivename, string filename)
		{
			return new UpdateData(archivename) {
				LocalFileName = filename
			};
		}
	}

	public class Exporter : IDisposable
	{
		private ILog log = LogManager.GetLogger(typeof(Exporter));

		private ISession session;

		private FileCleaner cleaner = new FileCleaner();
		private bool disposed;

		private User user;
		private UserSettings settings;
		private AnalitfNetData data;
		private Version version;

		public string UpdateType;

		public Config.Config Config;

		public string Prefix = "";
		public string ResultPath = "";
		public string AdsPath = "";
		public string UpdatePath;
		public string DocsPath = "";

		public uint MaxProducerCostPriceId;
		public uint MaxProducerCostCostId;

		public Exporter(ISession session, uint userId, Version version, string updateType = null)
		{
			this.session = session;
			this.version = version;
			this.UpdateType = updateType;
			user = session.Load<User>(userId);
			settings = session.Load<UserSettings>(userId);
			data = session.Get<AnalitfNetData>(user.Id);
		}

		//Все даты передаются в UTC!
		public void Export(List<UpdateData> result)
		{
			data = data ?? new AnalitfNetData(user);
			data.LastPendingUpdateAt = DateTime.Now;
			session.Save(data);

			session.CreateSQLQuery("drop temporary table if exists usersettings.prices;" +
				"drop temporary table if exists usersettings.activeprices;" +
				"call Customers.GetOffers(:userId);" +
				"call Customers.GetPrices(:userId);")
				.SetParameter("userId", user.Id)
				.ExecuteUpdate();

			CostOptimizer.OptimizeCostIfNeeded((MySqlConnection)session.Connection, user.Client.Id, user.Id);

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
	CauseRejects,
	CancelDate is not null as Canceled
from Farm.Rejects
union
select
	l.RejectId,
	l.Product,
	l.ProductId,
	l.Producer,
	l.ProducerId,
	l.Series,
	l.LetterNo,
	convert_tz(LetterDate, @@session.time_zone,'+00:00') as LetterDate,
	l.CauseRejects,
	1 as Canceled
from
	Logs.RejectLogs l
where
	l.LogTime >= ?lastUpdate
and l.Operation = 2";
			Export(result, sql, "Rejects", new { lastUpdate = data.LastUpdateAt });

			sql = @"
select a.Id,
a.Address as Name
from Customers.Addresses a
join Customers.UserAddresses ua on ua.AddressId = a.Id
where a.Enabled = 1 and ua.UserId = ?userId";
			Export(result, sql, "Addresses", new { userId = user.Id });

			sql = @"
select u.Id,
	u.InheritPricesFrom is not null as IsPriceEditDisabled,
	u.UseAdjustmentOrders as IsPreprocessOrders,
	c.FullName as FullName
from Customers.Users u
	join Customers.Clients c on c.Id = u.ClientId
where u.Id = ?userId";
			Export(result, sql, "Users", new { userId = user.Id });

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
			Export(result, sql, "MinOrderSumRules", new { userId = user.Id });

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
			CachedExport(result, sql, "ProductDescriptions");

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

			sql = @"
select Id, PublicationDate, Header
from Usersettings.News
where PublicationDate < curdate() + interval 1 day
	and Deleted = 0
	and DestinationType in (1, 2)";
			Export(result, sql, "News");

			var newses = session.CreateSQLQuery(@"select Id, Body
from Usersettings.News
where PublicationDate < curdate() + interval 1 day
	and Deleted = 0
	and DestinationType in (1, 2)")
				.List<object[]>();

			var template = "<html>"
				+ "<head>"
				+ "<meta charset=\"utf-8\">"
				+ "</head>"
				+ "<body>"
				+ "{0}"
				+ "<body>"
				+ "</html>";
			foreach (var news in newses) {
				var name = news[0] + ".html";
				result.Add(new UpdateData("newses/" + name) {
					Content = String.Format(template, news[1])
				});
			}

			ExportMails(result);
			ExportDocs(result);
			ExportOrders(result);
		}

		private void ExportMails(List<UpdateData> result)
		{
			session.CreateSQLQuery("delete from Logs.PendingMailLogs where UserId = :userId")
				.SetParameter("userId", user.Id)
				.ExecuteUpdate();

			var mails = session.CreateSQLQuery(@"
select m.Id,
	if(m.IsVIPMail and (m.SupplierEmail like '%@analit.net' or m.SupplierEmail like '%.analit.net'), 1, 0)
from documents.Mails m
	join Logs.MailSendLogs ms on ms.MailId = m.Id
	join Customers.Suppliers s on s.Id = m.SupplierId
where
	m.LogTime > curdate() - interval 30 day
	and ms.UserId = :userId
	and ms.Committed = 0
	and m.Deleted = 0
order by m.LogTime desc
limit 200;")
				.SetParameter("userId", user.Id)
				.List<object[]>();
			var ids = mails.Select(m => Convert.ToUInt32(m[0])).ToArray();
			var loadMaiIds = mails.Where(m => Convert.ToBoolean(m[1])).Select(m => Convert.ToUInt32(m[0])).ToArray();

			if (ids.Count() == 0)
				return;

			var pendingMails = session.Query<MailSendLog>().Where(l => ids.Contains(l.MailId)).ToArray()
				.Select(l => new PendingMailLog(l));

			var sql = String.Format(@"
select m.Id,
	convert_tz(m.LogTime, @@session.time_zone,'+00:00') as SentAt,
	m.IsVIPMail as IsSpecial,
	m.SupplierEmail as SenderEmail,
	s.Name as Sender,
	m.Subject,
	m.Body,
	1 as IsNew
from Documents.Mails m
	join customers.Suppliers s on s.Id = m.SupplierId
where m.Id in ({0})", ids.Implode());

			Export(result, sql, "mails", truncate: false);

			sql = String.Format(@"
select a.Id,
	a.Filename as Name,
	a.Size,
	a.MailId,
	if(m.IsVIPMail and (m.SupplierEmail like '%@analit.net' or m.SupplierEmail like '%.analit.net'), 1, 0) as IsDownloaded
from Documents.Attachments a
	join Documents.Mails m on m.Id = a.MailId
where a.MailId in ({0})", ids.Implode());

			Export(result, sql, "attachments", truncate: false);

			IEnumerable<Attachment> loadable = session.Query<Attachment>()
				.Where(a => loadMaiIds.Contains(a.MailId))
				.ToArray();
			result.AddRange(loadable.Select(attachment => UpdateData.FromFile(attachment.GetArchiveName(), attachment.GetFilename(Config))));
			session.SaveEach(pendingMails);
		}

		private void CachedExport(List<UpdateData> result, string sql, string tag)
		{
			if (Config == null) {
				Export(result, sql, tag);
				return;
			}

			var cacheData = Path.Combine(Config.CachePath, tag + ".txt");
			var cacheMeta = Path.Combine(Config.CachePath, tag + ".meta.txt");
			if (IsCacheStale(cacheData)) {
				Export(result, sql, tag);
				//если другая нитка успела обновить кеш раньше
				if (IsCacheStale(tag)) {
					var data = result.First(r => r.ArchiveFileName == tag + ".txt");
					File.Copy(data.LocalFileName, cacheData, true);
					var meta = result.First(r => r.ArchiveFileName == tag + ".meta.txt");
					File.WriteAllText(cacheMeta, meta.Content);
				}
			}
			else {
				result.Add(new UpdateData(tag + ".meta.txt") { LocalFileName = cacheMeta });
				result.Add(new UpdateData(tag + ".txt") { LocalFileName = cacheData });
			}
		}

		private bool IsCacheStale(string filename)
		{
			return new FileInfo(filename).LastWriteTime.Date != DateTime.Today;
		}

		private void ExportOrders(List<UpdateData> result)
		{
			if (!settings.AllowDownloadUnconfirmedOrders)
				return;

			session.CreateSQLQuery("delete from Logs.PendingOrderLogs where UserId = :userId")
				.SetParameter("userId", user.Id)
				.ExecuteUpdate();

			var addresses = user.AvaliableAddresses.Select(a => (uint?)a.Id).ToArray();
			var prices = session.Query<ActivePrice>().Select(p => p.Id).ToArray();
			var orders = session.Query<Order>()
				.Where(o => !o.Deleted
					&& !o.Processed
					&& !o.Submited
					&& o.UserId != user.Id
					&& addresses.Contains(o.AddressId))
				.ToArray();
			orders = orders.Where(o => prices.Contains(new PriceKey(o.PriceList, o.RegionCode))).ToArray();

			var groups = orders.GroupBy(o => new { o.AddressId, o.PriceList, o.RegionCode });
			foreach (var @group in groups) {
				foreach (var order in group) {
					session.Save(new PendingOrderLog(order, user, group.First().RowId));
				}
			}

			Export(result, "Orders",
				new[] {
					"ExportId",
					"CreatedOn",
					"AddressId",
					"PriceId",
					"RegionId",
					"Comment"
				},
				groups.Select(g => new object[] {
					g.First().RowId,
					g.First().WriteTime.ToUniversalTime(),
					g.Key.AddressId,
					g.Key.PriceList.PriceCode,
					g.Key.RegionCode,
					g.Implode(o => o.ClientAddition)
				}),
				false);

			var sql = @"
select l.ExportId as ExportOrderId,
	ol.Quantity as Count,
	p.Id as ProductId,
	p.CatalogId as CatalogId,
	ol.SynonymCode as ProductSynonymId,
	pr.Name as Producer,
	ol.CodeFirmCr as ProducerId,
	ol.SynonymFirmCrCode as ProducerSynonymId,
	ol.Code,
	ol.CodeCr,
	oo.Unit,
	oo.Volume,
	oo.Quantity,
	oo.Note,
	oo.Period,
	oo.Doc,
	ol.Junk,
	oo.MinBoundCost,
	oo.MaxBoundCost,
	oo.VitallyImportant,
	oo.RegistryCost,
	mx.Cost as MaxProducerCost,
	ol.RequestRatio,
	ol.OrderCost as MinOrderSum,
	ol.MinOrderCount,
	oo.ProducerCost,
	oo.NDS,
	ol.EAN13,
	ol.CodeOKP,
	ol.Series,
	st.Synonym as ProductSynonym,
	si.Synonym as ProducerSynonym,
	ol.Cost,
	oh.RegionCode as RegionId,
	ol.CoreId as OfferId
from Logs.PendingOrderLogs l
	join Orders.OrdersList ol on ol.OrderId = l.OrderId
		join Orders.OrdersHead oh on oh.RowId = ol.OrderId
		join Catalogs.Products p on p.Id = ol.ProductId
		left join farm.synonymArchive st on st.SynonymCode = ol.SynonymCode
		left join farm.synonymFirmCr si on si.SynonymFirmCrCode = ol.SynonymFirmCrCode
		left join Catalogs.Producers pr on pr.Id = ol.CodefirmCr
		left join Orders.OrderedOffers oo on oo.Id = ol.RowId
		left join Usersettings.MaxProducerCosts mx on mx.ProductId = ol.ProductId and mx.ProducerId = ol.CodeFirmCr
where l.UserId = ?userId
group by ol.RowId";
			Export(result, sql, "OrderLines", new { userId = user.Id }, false);
		}

		private void ExportDocs(List<UpdateData> result)
		{
			string sql;

			session.CreateSQLQuery(@"delete from Logs.PendingDocLogs"
				+ " where UserId = :userId;")
				.SetParameter("userId", user.Id)
				.ExecuteUpdate();

			var logs = session.Query<DocumentSendLog>()
				.Where(l => !l.Committed && l.User.Id == user.Id)
				.Take(400)
				.ToArray();

			if (logs.Length == 0) {
				//мы должны передать LoadedDocuments что бы клиент очистил таблицу
				Export(result,
					"LoadedDocuments",
					new[] { "Id", "Type", "SupplierId", "OriginFilename" },
					new object[0][]);
				return;
			}

			foreach (var log in logs) {
				log.Committed = false;
				log.FileDelivered = false;
				log.DocumentDelivered = false;
			}

			foreach (var doc in logs) {
				try {
					var type = doc.Document.DocumentType.ToString();
					var path = Path.Combine(DocsPath,
						doc.Document.AddressId.ToString(),
						type);
					if (!Directory.Exists(path))
						continue;
					var files = Directory.GetFiles(path, String.Format("{0}_*", doc.Document.Id));
					result.AddRange(files.Select(f => new UpdateData(Path.Combine(type, Path.GetFileName(f))) {
						LocalFileName = f
					}));
					if (files.Length > 0)
						doc.FileDelivered = true;
				}
				catch(Exception e) {
					log.Warn("Ошибка при экспорте файлов накладных", e);
				}
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
			Export(result, sql, "Waybills", new { userId = user.Id }, false);

			sql = String.Format(@"
select db.Id,
	d.RowId as WaybillId,
	db.ProductId,
	db.Product,
	db.ProducerId,
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
			Export(result, sql, "WaybillLines", new { userId = user.Id }, false);

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

			var delivered = logs.Where(l => l.DocumentDelivered || l.FileDelivered).ToArray();
			Export(result,
				"LoadedDocuments",
				new[] { "Id", "Type", "SupplierId", "OriginFilename", },
				delivered.Select(l => new object[] {
					l.Document.Id,
					(int)l.Document.DocumentType,
					l.Document.Supplier.Id,
					l.FileDelivered ? l.Document.Filename : null,
				}));

			var pending = delivered.Select(l => new PendingDocLog(l));
			session.SaveEach(pending);
		}

		public void Export(List<UpdateData> data, string name, string[] meta, IEnumerable<object[]> exportData, bool truncate = true)
		{
			var filename = Path.GetFullPath(Path.Combine(Config.LocalExportPath, Prefix + name + ".txt"));
			data.Add(BuildMeta(name, truncate, meta));
			data.Add(new UpdateData(name + ".txt") { LocalFileName = filename });
			cleaner.Watch(filename);
			using(var file = new StreamWriter(File.OpenWrite(filename), Encoding.GetEncoding(1251))) {
				MySqlHelper.Export(exportData, file);
			}
		}

		public void Export(List<UpdateData> data, string sql, string file, object parameters = null, bool truncate = true)
		{
			var dataAdapter = new MySqlDataAdapter(sql + " limit 0", (MySqlConnection)session.Connection);
			if (parameters != null)
				ObjectExtentions.ToDictionary(parameters).Each(k => dataAdapter.SelectCommand.Parameters.AddWithValue(k.Key, k.Value));

			var table = new DataTable();
			dataAdapter.Fill(table);
			var columns = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();

			var path = Path.GetFullPath(Path.Combine(Config.RemoteExportPath, Prefix + file + ".txt"));
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

			data.Add(BuildMeta(file, truncate, columns));
			data.Add(new UpdateData(file + ".txt") { LocalFileName = path });
		}

		private static UpdateData BuildMeta(string file, bool truncate, string[] columns)
		{
			var content = new StringWriter();
			if (truncate)
				content.WriteLine("truncate");
			columns.Each(c => content.WriteLine(c));
			var updateData = new UpdateData(file + ".meta.txt") { Content = content.ToString() };
			return updateData;
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
					var filename = tuple.LocalFileName;
					//экспоритровать пустые файлы важно тк пустой файл привед к тому что таблица бедет очищена
					//напимер в случае если последний адрес доставки был отключен
					if (String.IsNullOrEmpty(filename)) {
						var content = new MemoryDataSource(new MemoryStream(Encoding.UTF8.GetBytes(tuple.Content)));
						zip.Add(content, tuple.ArchiveFileName);
					}
					else if (File.Exists(filename)) {
						zip.Add(filename, tuple.ArchiveFileName);
					}
					else {
						log.WarnFormat("Не найден файл для экспорта {0}", filename);
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

			//hack: в сборке 12 ошибка, обходим ее
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
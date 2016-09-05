using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using AnalitF.Net.Service.Models.Inventory;
using Castle.Components.Validator;
using Common.Models;
using Common.Models.Helpers;
using Common.Models.Repositories;
using Common.MySql;
using Common.NHibernate;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Tools.Helpers;
using HtmlAgilityPack;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Linq;
using log4net;
using Microsoft.SqlServer.Server;
using SmartOrderFactory.Domain;
using MySqlHelper = Common.MySql.MySqlHelper;

namespace AnalitF.Net.Service.Models
{
	//исключение что бы сигнализировать об ошибке понятной пользователю
	//передается клиенту
	public class ExporterException : Exception
	{
		public ExporterException(string message, ErrorType errorType = ErrorType.None) : base(message)
		{
			ErrorType = errorType;
		}

		public ErrorType ErrorType { get; set; }
	}

	public class Offer3 : Offer2
	{
		public uint CatalogId;
		public string Producer;
		public decimal? MaxProducerCost;
		public string ProductSynonym;
		public string ProducerSynonym;
		public string Properties;
	}

	//результат подготовки может включать как файлы которые должны быть включен в архив
	//так и файлы которые не должны включаться в архив и должны передаваться как есть
	//в виде multipart content
	//это оптимизация что бы избежать архивирования бинарных файлов тк на практике в этом нет смысла
	public class ExternalRawFile
	{
		public string Dir;
		public string Name;
		public string Filename;


		public static ExternalRawFile FromDir(string name, string dir)
		{
			return new ExternalRawFile {
				Name = name,
				Dir = dir
			};
		}

		public static ExternalRawFile FromFile(string filename)
		{
			return new ExternalRawFile {
				Filename = filename
			};
		}

		public override string ToString()
		{
			return $"External file: {Filename}";
		}
	}

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

		public override string ToString()
		{
			if (!String.IsNullOrEmpty(LocalFileName))
				return ArchiveFileName + " - " + LocalFileName + " - " + new FileInfo(LocalFileName).Length;
			else
				return ArchiveFileName;
		}

		public string ReadContent()
		{
			if (Content != null)
				return Content;
			return File.ReadAllText(LocalFileName, Encoding.GetEncoding(1251));
		}
	}

	//накопительное, промоакции - в prgdata промоакции экспортируются всегда хотя нет причин почему так происходит
	//накопительное, минипочта - в prgdata при кумулятивном обновлении минипочта не сбрасывается, сбрасывается только если происходит
	//ограниченное кумулятивное обновление
	//todo - накопительное, накладные в prgdata отправленные накладные сбрасываются на основании даты полученной с клиента
	//накопительное, заказы - сбрасывается только в случае ограниченного кумулятивного
	public class Exporter : IDisposable
	{
		private ILog log = LogManager.GetLogger(typeof(Exporter));

		private ISession session;

		private FileCleaner cleaner = new FileCleaner();
		private bool disposed;

		private User user;
		private AnalitfNetData data;
		private UserSettings userSettings;
		private ClientSettings clientSettings;
		private OrderRules orderRules;
		private RequestLog job;

		public Config.Config Config;

		public string Prefix = "";
		public string ResultPath = "";
		public string AdsPath = "";
		public string DocsPath = "";
		public string DateTimeNow = DateTime.Now.ToString("yyyy.MM.dd");

		public uint MaxProducerCostPriceId;
		public uint MaxProducerCostCostId;

		public List<Order> Orders;
		public List<OrderBatchItem> BatchItems;
		public Address BatchAddress;
		public List<UpdateData> Result = new List<UpdateData>();
		public List<ExternalRawFile> External = new List<ExternalRawFile>();
		public Address[] Addresses;

		public Exporter(ISession session, Config.Config config, RequestLog job)
		{
			this.session = session;
			this.job = job;
			Prefix = job.Id.ToString();

			ResultPath = config.ResultPath;
			AdsPath = config.AdsPath;
			DocsPath = config.DocsPath;
			Config = config;
			MaxProducerCostPriceId = config.MaxProducerCostPriceId;
			MaxProducerCostCostId = config.MaxProducerCostCostId;

			user = session.Load<User>(job.User.Id);
			data = session.Get<AnalitfNetData>(user.Id);
			if (data == null) {
				data = new AnalitfNetData(job);
				session.Save(data);
			}
			userSettings = session.Load<UserSettings>(user.Id);
			clientSettings = session.Load<ClientSettings>(user.Client.Id);
			orderRules = session.Load<OrderRules>(user.Client.Id);
			Addresses = user.AvaliableAddresses.ToArray();
		}

		//Все даты передаются в UTC!
		public void Export()
		{
			Addresses = user.AvaliableAddresses.Intersect(Addresses).ToArray();
#if DEBUG
			//на случай если были созданы тестовые данные нужно перечитать конфиг
			Application.ReadDbConfig(Config);
#endif

			if (userSettings.CheckClientToken
				&& !String.IsNullOrEmpty(job.ClientToken)) {
				if (String.IsNullOrEmpty(data.ClientTokenV2))
					data.ClientTokenV2 = job.ClientToken;
				else if (data.ClientTokenV2 != job.ClientToken)
					throw new ExporterException("Обновление программы на данном компьютере запрещено. Пожалуйста, обратитесь в АналитФармация.",
						ErrorType.AccessDenied);
			}

			data.LastPendingUpdateAt = DateTime.Now;
			//при переходе на новую версию мы должны отдать все данные тк между версиями могла измениться схема
			//и если не отдать все то часть данных останется в старом состоянии а часть в новом,
			//что может привести к странным результатам
			if (data.ClientVersion != job.Version)
				data.Reset();

			data.ClientVersion = job.Version;
			if (job.LastSync != data.LastUpdateAt) {
				log.WarnFormat("Не совпала дата обновления готовим кумулятивное обновление," +
					" последние обновление на клиента {0}" +
					" последнее обновление на сервере {1}" +
					" не подтвержденное обновление {2}", data.LastUpdateAt, job.LastSync, data.LastPendingUpdateAt);
				data.Reset();
			}
			session.Save(data);

			//по умолчанию fresh = 1
			var cumulative = data.LastUpdateAt == DateTime.MinValue;
			session.CreateSQLQuery(@"
drop temporary table if exists usersettings.prices;
drop temporary table if exists usersettings.activeprices;
call Customers.GetOffers(:userId);
call Customers.GetPrices(:userId);

insert into Usersettings.AnalitFReplicationInfo (FirmCode, UserId, ForceReplication)
select i.FirmCode, :userId, 1
from (
	select p.FirmCode
	from UserSettings.Prices p
	left join Usersettings.AnalitFReplicationInfo r on r.FirmCode = p.FirmCode and r.UserId = :userId
	where r.UserId is null
	group by p.FirmCode
) as i;

update Usersettings.ActivePrices ap
	join Usersettings.AnalitFReplicationInfo r on r.FirmCode = ap.FirmCode
set ap.Fresh = (r.ForceReplication > 0 or :cumulative)
where r.UserId = :userId;

update Usersettings.ActivePrices ap
	join Usersettings.AnalitFReplicationInfo r on r.FirmCode = ap.FirmCode
set r.ForceReplication = 2
where r.UserId = :userId and ap.Fresh = 1;")
				.SetParameter("userId", user.Id)
				.SetParameter("cumulative", cumulative)
				//для тех записей которые мы создали время репликации должно быть меньше текущего что бы не реплицировать
				//данные еще раз
				.ExecuteUpdate();
			CostOptimizer.OptimizeCostIfNeeded((MySqlConnection)session.Connection, user.Client.Id, user.Id);

			string sql;

			if (clientSettings.AllowAnalitFSchedule) {
				//клиент хранит время обновления как TimeSpan
				//в базе это преобразуется в TimeSpan.Ticks
				//tick - это 100 наносекунд те 10^7
				sql = @"
SELECT
	s.Id,
	s.Hour * 60 * 60 * 1000 * 1000 * 10 + s.Minute * 60 * 1000 * 1000 * 10 as UpdateAt
FROM
	UserSettings.AnalitFSchedules s
WHERE
	s.ClientId = ?clientId
and s.Enable = 1";
				Export(Result, sql, "Schedules", truncate: true, parameters: new { clientId = user.Client.Id });
			}
			else {
				//если настройка отключена мы все равно должны экспортировать пустую таблицу
				//тк у клиента опция сначала опция могла быть включена а затем выключена
				//что бы отключение сработало нужно очистить таблицу
				Export(Result, "Schedules", new[] { "Id", "UpdateAt" }, Enumerable.Empty<object[]>());
			}

			if (cumulative) {
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
from Farm.Rejects";
				CachedExport(Result, sql, "Rejects");
			}
			else {
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
where UpdateTime >= ?lastUpdate
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
from Logs.RejectLogs l
where l.LogTime >= ?lastUpdate
and l.Operation = 2";
				Export(Result, sql, "Rejects", truncate: false, parameters: new { lastUpdate = data.LastUpdateAt });
			}

			var legalEntityCount = session.CreateSQLQuery(@"
select
	count(distinct le.Id)
from Customers.Addresses a
	join Customers.UserAddresses ua on ua.AddressId = a.Id
	join Billing.LegalEntities le on le.Id = a.LegalEntityId
where
	ua.UserId = :userId
	and a.Enabled = 1")
				.SetParameter("userId", user.Id)
				.UniqueResult<long>();
			var addressName = "a.Address";
			if (legalEntityCount > 1)
				addressName = "concat(le.Name, ', ', a.Address)";

			sql = String.Format(@"
select a.Id,
	{0} as Name,
	exists(select * from OrderSendRules.SmartOrderLimits l where l.AddressId = a.Id) as HaveLimits,
	le.FullName as Org
from Customers.Addresses a
	join Customers.UserAddresses ua on ua.AddressId = a.Id
	join Billing.LegalEntities le on le.Id = a.LegalEntityId
where a.Enabled = 1 and ua.UserId = ?userId", addressName);

			Export(Result, sql, "Addresses", truncate: true, parameters: new { userId = user.Id });

			sql = @"
select l.AddressId,
	p.PriceCode as PriceId,
	p.RegionCode as RegionId,
	l.Value + ifnull(l.Today, 0) as Value
from Customers.Addresses a
	join Customers.UserAddresses ua on ua.AddressId = a.Id
	join OrderSendRules.SmartOrderLimits l on l.AddressId = a.Id
	join Usersettings.Prices p on p.FirmCode = l.SupplierId
where a.Enabled = 1 and ua.UserId = ?userId";
			Export(Result, sql, "Limits", truncate: true, parameters: new { userId = user.Id });

			var contacts = session
				.CreateSQLQuery("select TechContact, TechOperatingMode from Farm.Regions where RegionCode = :id")
				.SetParameter("id", user.Client.RegionCode)
				.List<object[]>();
			var rawPhone = contacts.Select(d => d[0]).Cast<string>().FirstOrDefault();
			var rawHours = contacts.Select(d => d[1]).Cast<string>().FirstOrDefault();

			sql = @"
select u.Id,
	u.InheritPricesFrom is not null as IsPriceEditDisabled,
	u.UseAdjustmentOrders as IsPreprocessOrders,
	c.Name,
	c.FullName as FullName,
	rcs.AllowDelayOfPayment and u.ShowSupplierCost as ShowSupplierCost,
	rcs.AllowDelayOfPayment as IsDelayOfPaymentEnabled,
	?supportPhone as SupportPhone,
	?supportHours as SupportHours,
	?lastSync as LastSync,
	rcs.SaveOrders,
	exists(
		select *
		from Customers.UserAddresses ua
			join Customers.Addresses a on a.Id = ua.AddressId
			join OrderSendRules.SmartOrderLimits l on l.AddressId = a.Id
			join Usersettings.Prices p on p.FirmCode = l.SupplierId
		where ua.UserId = u.Id and a.Enabled = 1
	) as HaveLimits
from Customers.Users u
	join Customers.Clients c on c.Id = u.ClientId
	join UserSettings.RetClientsSet rcs on rcs.ClientCode = c.Id
where u.Id = ?userId";
			Export(Result, sql, "Users", truncate: true, parameters: new {
				userId = user.Id,
				supportPhone = HtmlToText(rawPhone),
				supportHours = HtmlToText(rawHours),
				lastSync = data.LastPendingUpdateAt
			});

			sql = @"
select up.Id,
	up.Shortcut as Name,
	a.UserId
from Usersettings.AssignedPermissions a
	join Usersettings.UserPermissions up on up.Id = a.PermissionId
where a.UserId = ?userId
	and up.Type in (1, 2)";
			Export(Result, sql, "Permissions", truncate: true, parameters: new { userId = user.Id });

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
	and a.Enabled = 1";
			Export(Result, sql, "MinOrderSumRules", truncate: true, parameters: new { userId = user.Id });

			CreateMaxProducerCosts();
			var maxProducerCostDate = session.CreateSQLQuery(@"
select pi.PriceDate
from Usersettings.PriceItems pi
join Usersettings.PricesCosts pc on pc.PriceItemId = pi.Id
where pc.CostCode = :costId")
				.SetParameter("costId", MaxProducerCostCostId)
				.List<DateTime?>()
				.FirstOrDefault();
			if (maxProducerCostDate.GetValueOrDefault() >= data.LastUpdateAt) {
				sql = @"select * from Usersettings.MaxProducerCosts";
				Export(Result, sql, "MaxProducerCosts", truncate: true);
			}

			sql = @"select
	s.Id,
	s.Name,
	if(length(s.FullName) = 0, s.Name, s.FullName) as FullName,
	exists(select * from documents.SourceSuppliers ss where ss.SupplierId = s.Id) HaveCertificates,
	s.VendorId
from Customers.Suppliers s
	join Usersettings.Prices p on p.FirmCode = s.Id";
			Export(Result, sql, "suppliers", truncate: true);

			//у mysql неделя начинается с понедельника у .net с воскресенья
			//приводим к виду .net
			sql = @"
select
	p.PriceCode as PriceId,
	p.RegionCode as RegionId,
	if(d.DayOfWeek = 7, 0, d.DayOfWeek + 1) as DayOfWeek,
	if(?delayOfPaymentEnabled, d.VitallyImportantDelay, 0) as VitallyImportantDelay,
	if(?delayOfPaymentEnabled, d.OtherDelay, 0) as OtherDelay
from (Usersettings.DelayOfPayments d, UserSettings.Prices p)
	join UserSettings.PriceIntersections pi on pi.Id = d.PriceIntersectionId and pi.PriceId = p.PriceCode
	join UserSettings.SupplierIntersection si on si.Id = pi.SupplierIntersectionId and si.SupplierID = p.FirmCode
where
	si.ClientId = ?clientId";
			Export(Result, sql, "DelayOfPayments", truncate: true, parameters: new {
				clientId = clientSettings.Id,
				delayOfPaymentEnabled = clientSettings.AllowDelayOfPayment
			});


			//для выборки данных используется кеш оптимизированных цен
			//кеш нужен что бы все пользователи одного клиента имели одинаковый набор цен
			//кеш перестраивается на основании даты прайс-листа, при выборке проверяется дата
			//если кеш актуален выбирается цена из кеша и позиция отмечается как неоптимизируемая
			//если кеш неактуален то производится оптимизация по завершении которой цена сохраняется см UpdateCostCache
			var offersQueryParts = new MatrixHelper(orderRules).BuyingMatrixCondition(false);
			sql = @"
	core.Id as OfferId,
	ct.RegionCode as RegionId,
	ct.PriceCode as PriceId,

	core.ProductId as ProductId,
	core.CodeFirmCr as ProducerId,
	core.SynonymCode as ProductSynonymId,
	core.SynonymFirmCrCode as ProducerSynonymId,
	core.Quantity,
	core.Code as Code,
	core.CodeCr as CodeCr,
	core.Junk as Junk,
	core.Await as Await,
	core.Unit as Unit,
	core.Volume as Volume,
	core.Note as Note,
	core.Period as Period,
	core.Doc as Doc,
	core.RegistryCost as RegistryCost,
	core.ProducerCost as ProducerCost,
	core.EAN13 as EAN13,
	core.CodeOKP as CodeOKP,
	core.Series as Series,

	ifnull(cc.RequestRatio, core.RequestRatio) as RequestRatio,
	ifnull(cc.MinOrderSum, core.OrderCost) as MinOrderSum,
	ifnull(cc.MinOrderCount, core.MinOrderCount) as MinOrderCount,
	products.CatalogId,
	pr.Name as Producer,
	mx.Cost as MaxProducerCost,
	core.VitallyImportant or catalog.VitallyImportant as VitallyImportant,
	s.Synonym as ProductSynonym,
	sfc.Synonym as ProducerSynonym,
	if(k.Id is null or k.Date < at.PriceDate, ct.Cost, ifnull(ca.Cost, ct.Cost)) as Cost,

	at.FirmCode as SupplierId,
	core.MaxBoundCost,
	if(k.Id is null or k.Date < at.PriceDate, core.OptimizationSkip, 1) as OptimizationSkip,
	core.Exp,
	products.Properties,
	Core.Nds
";
			sql += offersQueryParts.Select + "\r\n";
			var query = SqlQueryBuilderHelper.GetFromPartForCoreTable(offersQueryParts, false);
			query.Join(@"left join Catalogs.Producers pr on pr.Id = core.CodeFirmCr
left join Usersettings.MaxProducerCosts mx on mx.ProductId = core.ProductId and mx.ProducerId = core.CodeFirmCr
join farm.Synonym s on core.synonymcode = s.synonymcode
left join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = core.SynonymFirmCrCode
left join farm.CachedCostKeys k on k.PriceId = ct.PriceCode and k.RegionId = ct.RegionCode and k.ClientId = ?clientCode
	left join farm.CachedCosts ca on ca.CoreId = core.Id and ca.KeyId = k.Id");
			sql = query.Select(sql).ToSql();
			var offers = new List<Offer3>();
			var cmd = new MySqlCommand(sql, (MySqlConnection)session.Connection);
			cmd.Parameters.AddWithValue("userId", user.Id);
			cmd.Parameters.AddWithValue("clientCode", user.Client.Id);
			using(var reader = cmd.ExecuteReader()) {
				while (reader.Read()) {
					offers.Add(new Offer3 {
						OfferId = reader.GetUInt64(0),
						RegionId = reader.GetUInt64(1),
						PriceId = reader.GetUInt32(2),
						ProductId = reader.GetUInt32(3),
						ProducerId = reader.GetNullableUInt32(4),
						SynonymCode = reader.GetUInt32(5),
						SynonymFirmCrCode = reader.GetNullableUInt32(6),
						Quantity = reader.SafeGetString(7),
						Code = reader.SafeGetString(8),
						CodeCr = reader.SafeGetString(9),
						Junk = reader.GetBoolean(10),
						Await = reader.GetBoolean(11),
						Unit = reader.SafeGetString(12),
						Volume = reader.SafeGetString(13),
						Note = reader.SafeGetString(14),
						Period = reader.SafeGetString(15),
						Doc = reader.SafeGetString(16),
						RegistryCost = reader.GetNullableFloat(17),
						ProducerCost = reader.GetNullableFloat(18),
						EAN13 = reader.SafeGetString(19),
						CodeOKP = reader.SafeGetString(20),
						Series = reader.SafeGetString(21),
						RequestRatio = reader.GetNullableUInt32(22),
						OrderCost = reader.GetNullableUInt32(23),
						MinOrderCount = reader.GetNullableUInt32(24),
						CatalogId = reader.GetUInt32(25),
						Producer = reader.SafeGetString(26),
						MaxProducerCost = reader.GetNullableDecimal(27),
						VitallyImportant = reader.GetBoolean(28),
						ProductSynonym = reader.SafeGetString(29),
						ProducerSynonym = reader.SafeGetString(30),
						Cost = reader.GetDecimal(31),
						//поля для оптимизации цен
						SupplierId = reader.GetUInt32(32),
						MaxBoundCost = reader.GetNullableFloat(33),
						OptimizationSkip = reader.GetBoolean(34),
						Exp = reader.GetNullableDateTime(35),
						Properties = reader.GetNullableString(36),
						Nds = reader.GetNullableUInt32(37),

						BuyingMatrixType = reader.GetUInt32(38),
					});
				}
			}

			var prices = session.Query<ActivePrice>().ToArray();
			var supplierIds = prices.Select(p => p.Id.Price.Supplier.Id).Distinct().ToArray();
			var optimizer = MonopolisticsOptimizer.Load(session, user, supplierIds);
			optimizer.PatchFresh(prices);
			session.Flush();
			var logs = optimizer.Optimize(offers);
			optimizer.UpdateCostCache(session, prices, logs);
			CostOptimizer.SaveLogs((MySqlConnection)session.Connection, logs, user);
			var freshPrices = prices.Where(p => p.Fresh).Select(p => p.Id.Price.PriceCode).OrderBy(i => i).ToArray();
			IEnumerable<Offer3> toExport = offers;
			if (!cumulative)
				toExport = offers.Where(o => Array.BinarySearch(freshPrices, o.PriceId) >= 0);
			Export(Result, "offers", new[] {
				"OfferId",
				"RegionId",
				"PriceId",
				"ProductId",
				"ProducerId",
				"ProductSynonymId",
				"ProducerSynonymId",
				"Quantity",
				"Code",
				"CodeCr",
				"Junk",
				"Unit",
				"Volume",
				"Note",
				"Period",
				"Doc",
				"RegistryCost",
				"ProducerCost",
				"CodeOKP",
				"Series",
				"RequestRatio",
				"MinOrderSum",
				"MinOrderCount",
				"CatalogId",
				"Producer",
				"MaxProducerCost",
				"VitallyImportant",
				"ProductSynonym",
				"ProducerSynonym",
				"Cost",
				"BuyingMatrixType",
				"Exp",
				"BarCode",
				"Properties",
				"Nds",
				"OriginalJunk"
			}, toExport.Select(o => new object[] {
				o.OfferId,
				o.RegionId,
				o.PriceId,
				o.ProductId,
				o.ProducerId,
				o.SynonymCode,
				o.SynonymFirmCrCode,
				o.Quantity,
				o.Code,
				o.CodeCr,
				o.Junk,
				o.Unit,
				o.Volume,
				o.Note,
				o.Period,
				o.Doc,
				o.RegistryCost,
				o.ProducerCost,
				o.CodeOKP,
				o.Series,
				o.RequestRatio,
				o.OrderCost,
				o.MinOrderCount,
				o.CatalogId,
				o.Producer,
				o.MaxProducerCost,
				o.VitallyImportant,
				o.ProductSynonym,
				o.ProducerSynonym,
				o.Cost,
				o.BuyingMatrixType,
				o.Exp,
				o.EAN13,
				o.Properties,
				o.Nds,
				o.Junk,
			}), truncate: cumulative);

			//экспортируем прайс-листы после предложений тк оптимизация может изменить fresh
			sql = @"select
	p.PriceCode as PriceId,
	p.PriceName as PriceName,
	s.Name as Name,
	r.RegionCode as RegionId,
	r.Region as RegionName,
	s.Id as SupplierId,
	s.Name as SupplierName,
	s.FullName as SupplierFullName,
	ifnull(rd.Storage, 0) as Storage,
	if(p.DisabledByClient or not p.Actual,
		0,
		(select count(*)
			from UserSettings.Core c where c.PriceCode = p.PriceCode and c.RegionCode = p.RegionCode)) as PositionCount,
	convert_tz(p.PriceDate, @@session.time_zone,'+00:00') as PriceDate,
	rd.OperativeInfo,
	rd.ContactInfo,
	rd.SupportPhone as Phone,
	rd.AdminMail as Email,
	p.FirmCategory as Category,
	p.DisabledByClient,
	ifnull(ap.Fresh, 1) IsSynced,
	((u.OrderRegionMask & c.MaskRegion & r.OrderRegionMask & p.RegionCode) = 0) as IsOrderDisabled,
	p.MainFirm as BasePrice,
	(p.OtherDelay + 100) / 100 as CostFactor,
	(p.VitallyImportantDelay + 100) / 100 as VitallyImportantCostFactor,
	p.CostCode as CostId,
	pc.CostName
from (Usersettings.Prices p, Customers.Users u)
	join Usersettings.PricesData pd on pd.PriceCode = p.PriceCode
		join Customers.Suppliers s on s.Id = pd.FirmCode
	join Farm.Regions r on r.RegionCode = p.RegionCode
	join Usersettings.RegionalData rd on rd.FirmCode = s.Id and rd.RegionCode = r.RegionCode
	left join Usersettings.ActivePrices ap on ap.PriceCode = p.PriceCode and ap.RegionCode = p.RegionCode
	join Customers.Clients c on c.Id = u.ClientId
	join Usersettings.RetClientsSet r on r.ClientCode = c.Id
	join Usersettings.PricesCosts pc on pc.CostCode = p.CostCode
where u.Id = ?userId";
			Export(Result, sql, "prices", truncate: true, parameters: new { userId = user.Id });

			if (cumulative) {
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
	Composition,
	pku.Narcotic as Narcotic,
	pku.Toxic as Toxic,
	pku.Combined as Combined,
	pku.Other as Other
from Catalogs.Descriptions d
join (
	select cn.DescriptionId,
		max(cat.Narcotic) as Narcotic,
		max(cat.Toxic) as Toxic,
		max(cat.Combined) as Combined,
		max(cat.Other) as Other
	from Catalogs.Catalog cat
			join Catalogs.CatalogNames cn on cat.NameId = cn.Id
	where cat.Hidden = 0
	group by cn.DescriptionId
) pku on pku.DescriptionId = d.Id
where d.NeedCorrect = 0";
				CachedExport(Result, sql, "ProductDescriptions");
			}
			else {
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
	Composition,
	NeedCorrect as Hidden,
	pku.Narcotic as Narcotic,
	pku.Toxic as Toxic,
	pku.Combined as Combined,
	pku.Other as Other
from Catalogs.Descriptions d
join (
	select cn.DescriptionId,
		max(cat.Narcotic) as Narcotic,
		max(cat.Toxic) as Toxic,
		max(cat.Combined) as Combined,
		max(cat.Other) as Other,
		max(cat.UpdateTime) as CatalogUpdateTime,
		max(cn.UpdateTime) as CatalogNameUpdateTime
	from Catalogs.Catalog cat
			join Catalogs.CatalogNames cn on cat.NameId = cn.Id
	where cat.Hidden = 0
	group by cn.DescriptionId
) pku on pku.DescriptionId = d.Id
where d.UpdateTime > ?lastSync or pku.CatalogUpdateTime > ?lastSync or pku.CatalogNameUpdateTime > ?lastSync
union
select
	l.DescriptionId,
	null as Name,
	null as EnglishName,
	null as Description,
	null as Interaction,
	null as SideEffect,
	null as IndicationsForUse,
	null as Dosing,
	null as Warnings,
	null as ProductForm,
	null as PharmacologicalAction,
	null as Storage,
	null as Expiration,
	null as Composition,
	1 as Hidden,
	null as Narcotic,
	null as Toxic,
	null as Combined,
	null as Other
from logs.DescriptionLogs l
where l.LogTime >= ?lastSync and l.Operation = 2";
				Export(Result, sql, "ProductDescriptions", truncate: false, parameters: new { lastSync = data.LastUpdateAt });
			}

			if (cumulative) {
				sql = @"
select Id, Mnn as Name
from Catalogs.Mnn m";
				CachedExport(Result, sql, "mnns");
			}
			else {
				sql = @"
select l.MnnId as Id, l.Mnn as Name, 1 as Hidden
from logs.MnnLogs l
where l.LogTime >= ?lastSync and l.Operation = 2
union
select Id, Mnn as Name, 0 as Hidden
from Catalogs.Mnn m
where m.UpdateTime > ?lastSync";
				Export(Result, sql, "mnns", truncate: false, parameters: new { lastSync = data.LastUpdateAt });
			}

			if (cumulative) {
				sql = @"
select cn.Id,
	cn.Name,
	if(d.NeedCorrect = 1, null, cn.DescriptionId) as DescriptionId,
	cn.MnnId,
	pku.VitallyImportant,
	pku.MandatoryList,
	pku.Narcotic,
	pku.Toxic,
	pku.Combined,
	pku.Other
from Catalogs.CatalogNames cn
	join Catalogs.Catalog c on c.NameId = cn.Id
	join (
		select cat.NameId,
			max(cat.Narcotic) as Narcotic,
			max(cat.Toxic) as Toxic,
			max(cat.Combined) as Combined,
			max(cat.Other) as Other,
			max(cat.MandatoryList) as MandatoryList,
			max(cat.VitallyImportant) as VitallyImportant
		from Catalogs.Catalog cat
		where cat.Hidden = 0
		group by cat.NameId
	) pku on pku.NameId = cn.Id
	left join Catalogs.Descriptions d on d.Id = cn.DescriptionId
group by cn.Id
having sum(if(c.Hidden, 0, 1)) > 0";
				CachedExport(Result, sql, "catalognames");
			}
			else {
				sql = @"
select cn.Id,
	cn.Name,
	if(d.NeedCorrect = 1, null, cn.DescriptionId) as DescriptionId,
	cn.MnnId,
	pku.VitallyImportant,
	pku.MandatoryList,
	pku.Narcotic,
	pku.Toxic,
	pku.Combined,
	pku.Other,
	not exists(select * from Catalogs.Catalog cat where cat.NameId = cn.Id and cat.Hidden = 0) as Hidden
from Catalogs.CatalogNames cn
	join Catalogs.Catalog c on c.NameId = cn.Id
	join (
		select cat.NameId,
			max(cat.Narcotic) as Narcotic,
			max(cat.Toxic) as Toxic,
			max(cat.Combined) as Combined,
			max(cat.Other) as Other,
			max(cat.MandatoryList) as MandatoryList,
			max(cat.VitallyImportant) as VitallyImportant,
			max(cat.UpdateTime) as CatalogUpdateTime
		from Catalogs.Catalog cat
		where cat.Hidden = 0
		group by cat.NameId
	) pku on pku.NameId = cn.Id
	left join Catalogs.Descriptions d on d.Id = cn.DescriptionId
where cn.UpdateTime > ?lastSync
	or d.UpdateTime > ?lastSync
	or pku.CatalogUpdateTime > ?lastSync
	or c.UpdateTime > ?lastSync
group by cn.Id";
				Export(Result, sql, "catalognames", truncate: false, parameters: new { lastSync = data.LastUpdateAt });
			}

			if (cumulative) {
				sql = @"
select
	c.Id,
	c.NameId,
	c.VitallyImportant,
	c.MandatoryList,
	cf.Id as FormId,
	cf.Form as Form,
	c.Name as Fullname,
	c.Narcotic,
	c.Toxic,
	c.Combined,
	c.Other
from Catalogs.Catalog c
	join Catalogs.CatalogForms cf on cf.Id = c.FormId
where c.Hidden = 0";
				CachedExport(Result, sql, "catalogs");
			}
			else {
				sql = @"
select
	c.Id,
	c.NameId,
	c.VitallyImportant,
	c.MandatoryList,
	cf.Id as FormId,
	cf.Form as Form,
	c.Hidden,
	c.Name as Fullname,
	c.Narcotic,
	c.Toxic,
	c.Combined,
	c.Other
from Catalogs.Catalog c
	join Catalogs.CatalogForms cf on cf.Id = c.FormId
where c.UpdateTime > ?lastSync";
				Export(Result, sql, "catalogs", truncate: false, parameters: new { lastSync = data.LastUpdateAt });
			}

			if (cumulative) {
				sql = @"
select Id, CatalogId, Hidden
from Catalogs.Products
where Hidden = 0";
				CachedExport(Result, sql, "Products");
			} else {
				sql = @"
select Id, CatalogId, Hidden
from Catalogs.Products
where Hidden = 0 and UpdateTime > ?lastSync";
				Export(Result, sql, "Products", truncate: false, parameters: new { lastSync = data.LastUpdateAt });
			}

			if (cumulative) {
				sql = @"
select p.Id, p.Name
from Catalogs.Producers p";
				CachedExport(Result, sql, "producers");
			}
			else {
				sql = @"
select l.ProducerId as Id, l.Name, 1 as Hidden
from logs.ProducerLogs l
where l.LogTime >= ?lastSync and l.Operation = 2
union
select p.Id, p.Name, 0 as Hidden
from Catalogs.Producers p
where p.UpdateTime > ?lastSync";
				Export(Result, sql, "producers", truncate: false, parameters: new { lastSync = data.LastUpdateAt });
			}

			var lastFormalization = session.CreateSQLQuery(@"
select if(LastFormalization > PriceDate, LastFormalization, PriceDate)
from usersettings.priceitems i
	join Usersettings.PricesCosts pc on pc.PriceItemId = i.Id
		join Usersettings.PricesData pd on pd.PriceCode = pc.PriceCode
where pd.PriceCode = :priceId;")
				.SetParameter("priceId", Config.RegulatorRegistryPriceId)
				.List<DateTime?>()
				.FirstOrDefault();
			if (cumulative || data.LastUpdateAt < lastFormalization) {
				sql = @"
select c.Id,
	c.Code as DrugID,
	c.Note as InnR,
	c.Doc as TradeNmR,
	c.Series as DrugFmNmRS,
	c.Unit as Pack,
	c.Volume as DosageR,
	s.Synonym as ClNm,
	c.CodeCr as Segment,
	c.ProductId,
	c.CodeFirmCr as ProducerId
from Farm.Core0 c
	join Farm.SynonymFirmCr s on s.SynonymFirmCrCode = c.SynonymFirmCrCode
where c.PriceCode = ?priceId and c.CodeFirmCr is not null";
				Export(Result, sql, "RegulatorRegistry", truncate: true,
					parameters: new { priceId = Config.RegulatorRegistryPriceId });
			}

			var lastDataUpdate = ((MySqlConnection)session.Connection)
				.Read<DateTime>("select Max(LastUpdate) from Reports.Drugs")
				.FirstOrDefault();
			if (lastDataUpdate > data.LastUpdateAt) {
				sql = @"
select DrugId,
  TradeNmR,
  InnR,
  PackNx,
  DosageR,
  PackQn,
  Pack,
  DrugFmNmRS,
  Segment,
  Year,
  Month,
  Series,
  TotDrugQn,
  MnfPrice,
  PrcPrice,
  RtlPrice,
  Funds,
  VendorID,
  Remark,
  SrcOrg,
  EAN,
  MaxMnfPrice,
  ExpiTermR,
  ClNm,
  MnfNm,
  PckNm,
  RegNr,
  RegDate
from Reports.Drugs";
				Export(Result, sql, "Drugs", truncate: true);
			}

			lastDataUpdate = ((MySqlConnection)session.Connection)
				.Read<DateTime>("select Max(LastUpdate) from Documents.BarCodes")
				.FirstOrDefault();
			if (lastDataUpdate > data.LastUpdateAt) {
				sql = @"
select Id, BarCode as Value
from Documents.BarCodes
where BarCode <> ''";
				Export(Result, sql, "BarCodes", truncate: true);
			}

			IList<object[]> newses = new List<object[]>();
			if (cumulative) {
				sql = @"
select Id, PublicationDate, Header
from Usersettings.News
where PublicationDate < curdate() + interval 1 day
	and DestinationType in (1, 2)
	and Deleted = 0";
				CachedExport(Result, sql, "News");

				newses = session.CreateSQLQuery(@"select Id, Body
from Usersettings.News
where PublicationDate < curdate() + interval 1 day
	and Deleted = 0
	and DestinationType in (1, 2)")
					.List<object[]>();
			}
			else {
				sql = @"
select Id, PublicationDate, Header, Deleted as Hidden
from Usersettings.News
where PublicationDate < curdate() + interval 1 day
	and DestinationType in (1, 2)
	and UpdateTime > ?lastSync";
				Export(Result, sql, "News", truncate: false, parameters: new { lastSync = data.LastUpdateAt });

				newses = session.CreateSQLQuery(@"select Id, Body
from Usersettings.News
where PublicationDate < curdate() + interval 1 day
	and Deleted = 0
	and DestinationType in (1, 2)
	and UpdateTime > :lastSync")
					.SetParameter("lastSync", data.LastUpdateAt)
					.List<object[]>();
			}

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
				Result.Add(new UpdateData("newses/" + name) {
					Content = String.Format(template, news[1])
				});
			}

			ExportPromotions();
			ExportProducerPromotions();
			ExportMails();
			ExportDocs();
			ExportOrders();
			//выбираем sql запросы которые будут выполнены на клиенте что бы в случае аварии починить базу клиента
			try {
				if (Directory.Exists(Config.PerUserSqlPath)) {
					var content = Directory.GetFiles(Config.PerUserSqlPath, job.User.Id + ".*")
						.SelectMany(f => File.ReadLines(f))
						.Implode(Environment.NewLine);
					if (!String.IsNullOrEmpty(content)) {
						Result.Add(new UpdateData("cmds") {
							Content = content
						});
					}
				}
			}
			catch(Exception e) {
				log.Error("Не удалось выбрать дополнительные sql команды", e);
			}
		}

		private string HtmlToText(string value)
		{
			if (String.IsNullOrEmpty(value))
				return "";
			try {
				var doc = new HtmlDocument();
				doc.LoadHtml(value);
				return HtmlEntity.DeEntitize(doc.DocumentNode.InnerText);
			}
			catch(Exception e) {
				log.Error($"Ошибка при преобразовании значения {value}", e);
				return "";
			}
		}

		private void CreateMaxProducerCosts()
		{
			string sql;
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
		}

		private void ExportPromotions()
		{
			if (!clientSettings.ShowAdvertising) {
				Export(Result, "PromotionCatalogs", new[] { "CatalogId", "PromotionId" }, Enumerable.Empty<object[]>());
				Export(Result, "Promotions", new[] { "Id" }, Enumerable.Empty<object[]>());
				return;
			}

			var ids = session.CreateSQLQuery(@"
select Id
from usersettings.SupplierPromotions sp
	join Usersettings.Prices p on p.FirmCode = sp.SupplierId
where sp.Status = 1").List<uint>();

			string sql;
			sql = @"
select
	sp.Id,
	sp.SupplierId,
	sp.Name,
	sp.Annotation
from usersettings.SupplierPromotions sp
	join Usersettings.Prices p on p.FirmCode = sp.SupplierId
where sp.Status = 1";
			Export(Result, sql, "Promotions", truncate: true);
			sql = @"
select
	pc.CatalogId,
	pc.PromotionId
from usersettings.PromotionCatalogs pc
	join usersettings.SupplierPromotions sp on pc.PromotionId = sp.Id
		join Usersettings.Prices p on p.FirmCode = sp.SupplierId
where sp.Status = 1";
			Export(Result, sql, "PromotionCatalogs", truncate: true);

			var promotions = session.Query<Promotion>().Where(p => ids.Contains(p.Id)).ToArray();
			if (Directory.Exists(Config.PromotionsPath)) {
				foreach (var promotion in promotions) {
					var local = promotion.GetFilename(Config);
					if (String.IsNullOrEmpty(local))
						continue;
					Result.Add(UpdateData.FromFile(promotion.GetArchiveName(local), local));
				}
			}
		}

		public void ExportProducerPromotions()
		{
			var sql = @"select pr.Id, pr.ProducerId, pr.Name, pr.Annotation, pr.PromoFileId, pr.RegionMask
									from ProducerInterface.Promotions pr
									where pr.Begin <= ?DT AND pr.Enabled = 1 AND pr.Status = 1";

			Export(Result, sql, "ProducerPromotions", truncate: true, parameters: new { DT = DateTimeNow });

			sql = @"select Promo.Id PromotionId, PromoGrug.DrugId CatalogId from ProducerInterface.promotions Promo
							left join ProducerInterface.promotionToDrug PromoGrug ON Promo.Id = PromoGrug.PromotionId
							Where Promo.Enabled = 1 AND Promo.`Status` = 1 AND Promo.Begin <= ?DT";

			Export(Result, sql, "ProducerPromotionCatalogs", truncate: true, parameters: new { DT = DateTimeNow });

			sql = @"select PR.Id PromotionId, PTS.SupplierId SupplierId
							from ProducerInterface.Promotions PR
							RIGHT JOIN ProducerInterface.promotionstosupplier PTS ON PTS.PromotionId = PR.Id";

			Export(Result, sql, "ProducerPromotionSuppliers", truncate: true);

			// Получаем список ID актуальных файлов в БД привязанных к промоакциям производителей

			var ids = session
				.CreateSQLQuery(@"select PromoFileId from ProducerInterface.Promotions Where Enabled = 1 AND Status = 1 AND Begin <= :DateTimeNow AND PromoFileId IS NOT NULL")
				.SetParameter("DateTimeNow", DateTimeNow).List<int>();

			ProducerPromotion producerPromotion = new ProducerPromotion();

			foreach (var FileId in ids)
			{
				producerPromotion.Id = FileId;
				sql = String.Format(@"select ImageName from ProducerInterface.MediaFiles Where Id = {0}", FileId);
				producerPromotion.Type = session.CreateSQLQuery(sql).List<string>().First().Split(new Char[] { '.' }).Last();
				var local = producerPromotion.GetFilename(Config);

				// Экспортируем файлы из БД если ранее они не скачивались

				if (!File.Exists(local))
				{
					var Query = session.CreateSQLQuery("select ImageFile from ProducerInterface.MediaFiles Where Id=:FileId");
					Query.SetParameter("FileId", FileId);
					byte[] FileBytes = (byte[])Query.UniqueResult();

					using (FileStream SW = new FileStream(local, FileMode.OpenOrCreate))
					{
						SW.Write(FileBytes, 0, FileBytes.Length);
					}
				}

				Result.Add(UpdateData.FromFile(producerPromotion.GetArchiveName(local), local));
			}
		}

		private void ExportMails()
		{
			session.CreateSQLQuery("delete from Logs.PendingMailLogs where UserId = :userId")
				.SetParameter("userId", user.Id)
				.ExecuteUpdate();

			var mails = session.CreateSQLQuery(@"
select m.Id,
	ms.Id as LogId,
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
			var logIds = mails.Select(m => Convert.ToUInt32(m[1])).ToArray();
			var loadMaiIds = mails.Where(m => Convert.ToBoolean(m[2])).Select(m => Convert.ToUInt32(m[0])).ToArray();

			if (ids.Length == 0)
				return;

			var pendingMails = session.Query<MailSendLog>().Where(l => logIds.Contains(l.Id)).ToArray()
				.Select(l => new PendingMailLog(l));

			var sql = $@"
select m.Id,
	convert_tz(m.LogTime, @@session.time_zone,'+00:00') as SentAt,
	m.IsVIPMail as IsSpecial,
	m.SupplierEmail as SenderEmail,
	s.Name as Sender,
	m.Subject,
	m.Body
from Documents.Mails m
	join customers.Suppliers s on s.Id = m.SupplierId
where m.Id in ({ids.Implode()})";

			Export(Result, sql, "mails", truncate: false);

			sql = $@"
select a.Id,
	a.Filename as Name,
	a.Size,
	a.MailId
from Documents.Attachments a
	join Documents.Mails m on m.Id = a.MailId
where a.MailId in ({ids.Implode()})";

			Export(Result, sql, "attachments", truncate: false);

			IEnumerable<Attachment> loadable = session.Query<Attachment>()
				.Where(a => loadMaiIds.Contains(a.MailId))
				.ToArray();
			Result.AddRange(loadable.Select(attachment => UpdateData.FromFile(attachment.GetArchiveName(), attachment.GetFilename(Config))));
			session.SaveEach(pendingMails);
		}

		private void CachedExport(List<UpdateData> result, string sql, string tag)
		{
			if (Config == null) {
				Export(result, sql, tag, truncate: true);
				return;
			}

			var cacheData = Path.Combine(Config.CachePath, tag + ".txt");
			var cacheMeta = Path.Combine(Config.CachePath, tag + ".meta.txt");
			if (IsCacheStale(cacheData)) {
				Export(result, sql, tag, truncate: true);
				//если другая нитка успела обновить кеш раньше
				if (IsCacheStale(tag)) {
					var data = result.First(r => r.ArchiveFileName == tag + ".txt");
					//если мы оперируем с удаленной шарой то там файл появится с задержкой
					FileHelper.Persistent<FileNotFoundException>(() => File.Copy(data.LocalFileName, cacheData, true));
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

		private void ExportOrders()
		{
			if (Orders != null) {
				Export(Result, "Orders",
					new[] {
						"ExportId",
						"CreatedOn",
						"AddressId",
						"PriceId",
						"RegionId",
						"Comment",
						"SkipRestore"
					},
					Orders.Select(g => new object[] {
						g.RowId,
						g.WriteTime.ToUniversalTime(),
						g.AddressId,
						g.PriceList.PriceCode,
						g.RegionCode,
						g.ClientAddition,
						//для автозаказа нужно игнорировать восстановление
						BatchItems != null,
					}),
					false);

				var connection = (MySqlConnection)session.Connection;
				var items = Orders.SelectMany(o => o.OrderItems);
				var productSynonymLookup = connection
					.Read(String.Format("select SynonymCode, Synonym from farm.Synonym where SynonymCode in ({0})",
						items.Select(i => i.SynonymCode.GetValueOrDefault()).DefaultIfEmpty(0u).Implode()))
					.ToLookup(r => (uint?)Convert.ToUInt32(r["SynonymCode"]), r => r["Synonym"].ToString());

				var producerSynonymLookup = connection
					.Read(String.Format("select SynonymFirmCrCode, Synonym from farm.SynonymFirmCr where SynonymFirmCrCode in ({0})",
						items.Select(i => i.SynonymFirmCrCode.GetValueOrDefault()).DefaultIfEmpty(0u).Implode()))
					.ToLookup(r => (uint?)Convert.ToUInt32(r["SynonymFirmCrCode"]), r => r["Synonym"].ToString());

				var producerIds = items.Select(i => i.CodeFirmCr)
					.Union(BatchItems.Select(b => b.Item).Where(i => i != null).Select(i => i.ProducerId))
					.Where(i => i != null)
					.Distinct();
				var producerLookup = connection
					.Read(String.Format("select Id, Name from Catalogs.Producers where Id in ({0})",
						producerIds.DefaultIfEmpty((uint?)0).Implode()))
					.ToLookup(r => Convert.ToUInt32(r["Id"]), r => r["Name"].ToString());

				var maxProducerCost = connection
					.Read("select * from Usersettings.MaxProducerCosts")
					.ToLookup(r => Tuple.Create((uint)r["ProductId"], r.GetNullableUInt32("ProducerId")), r => r.GetNullableDecimal("Cost"));

				var productIds = items.Select(i => i.ProductId);
				if (BatchItems != null)
					productIds = productIds.Concat(BatchItems.Where(x => x.Item != null).Select(i => i.Item.ProductId));
				productIds = productIds.Distinct().ToArray();

				var productLookup = connection
					.Read(String.Format("select Id, CatalogId, Properties from Catalogs.Products where Id in ({0})",
						productIds.DefaultIfEmpty(0u).Implode()))
					.ToLookup(r => (uint?)Convert.ToUInt32(r["Id"]), r => Tuple.Create(r["CatalogId"], r["Properties"]));

				var orderbatchLookup = (from batch in (BatchItems ?? new List<OrderBatchItem>())
					where batch.Item != null
					from line in batch.Item.OrderItems
					select Tuple.Create(batch, line))
					.ToLookup(t => t.Item2, t => t.Item1);
				Export(Result, "OrderLines",
					new[] {
						"ExportOrderId",
						"ExportId",
						"Count",
						"ProductId",
						"CatalogId",
						"ProductSynonymId",
						"Producer",
						"ProducerId",
						"ProducerSynonymId",
						"Code",
						"CodeCr",
						"Unit",
						"Volume",
						"Quantity",
						"Note",
						"Period",
						"Doc",
						"VitallyImportant",
						"RegistryCost",
						"MaxProducerCost",
						"RequestRatio",
						"MinOrderSum",
						"MinOrderCount",
						"ProducerCost",
						"NDS",
						"CodeOKP",
						"Series",
						"ProductSynonym",
						"ProducerSynonym",
						"Cost",
						"RegionId",
						"OfferId",
						"ExportBatchLineId",
						"Junk",
						"BarCode",
						"OriginalJunk",
					},
					items
						.Select(i => new object[] {
							i.Order.RowId,
							i.RowId,
							i.Quantity,
							i.ProductId,
							productLookup[i.ProductId].Select(x => x.Item1).FirstOrDefault(),
							i.SynonymCode,
							producerLookup[i.CodeFirmCr.GetValueOrDefault()].FirstOrDefault(),
							i.CodeFirmCr,
							i.SynonymFirmCrCode,
							i.Code,
							i.CodeCr,
							i.OfferInfo.Unit,
							i.OfferInfo.Volume,
							i.OfferInfo.Quantity,
							i.OfferInfo.Note,
							i.OfferInfo.Period,
							i.OfferInfo.Doc,
							i.OfferInfo.VitallyImportant,
							i.OfferInfo.RegistryCost,
							maxProducerCost[Tuple.Create(i.ProductId, i.CodeFirmCr)].FirstOrDefault(),
							i.RequestRatio,
							i.OrderCost,
							i.MinOrderCount,
							i.OfferInfo.ProducerCost,
							i.OfferInfo.NDS,
							i.CodeOKP,
							i.Series,
							productSynonymLookup[i.SynonymCode.GetValueOrDefault()].FirstOrDefault(),
							producerSynonymLookup[i.SynonymFirmCrCode.GetValueOrDefault()].FirstOrDefault(),
							i.Cost,
							i.Order.RegionCode,
							i.CoreId,
							orderbatchLookup[i].Select(x => (object)x.GetHashCode()).FirstOrDefault(),
							//уценка с учтом клиентских настроек, будет пересчитана на клиенте
							i.Junk,
							i.EAN13,
							//оригинальная уценка
							i.Junk,
						}), truncate: false);

				if (BatchItems != null) {
					Export(Result, "BatchLines",
						new[] {
							"ExportId",
							"Code",
							"CodeCr",
							"SupplierDeliveryId",
							"AddressId",
							"ProductSynonym",
							"ProductId",
							"CatalogId",
							"ProducerSynonym",
							"ProducerId",
							"Producer",
							"Quantity",
							"Comment",
							"Status",
							"Priority",
							"BaseCost",
							"Properties",
							"ServiceFields",
						},
						BatchItems.Select(i => new object[] {
							i.GetHashCode(),
							i.Code,
							i.CodeCr,
							i.SupplierDeliveryId,
							GetAddressId(BatchAddress, i),
							i.ProductName,
							i.Item?.ProductId,
							i.Item?.CatalogId,
							i.ProducerName,
							i.Item?.ProducerId,
							i.Item == null ? null : producerLookup[i.Item.ProducerId.GetValueOrDefault()].FirstOrDefault(),
							i.Quantity,
							i.Item == null ? i.Comment : i.Item.Comments.Implode(Environment.NewLine),
							i.Item == null ? (int)ItemToOrderStatus.NotOrdered : (int)i.Item.Status,
							i.Priority,
							i.BaseCost,
							i.Item != null ? productLookup[i.Item.ProductId].Select(x => x.Item2).FirstOrDefault() : null,
							JsonConvert.SerializeObject(i.ServiceValues)
						}), truncate: false);
				}

				return;
			}
			if (!userSettings.AllowDownloadUnconfirmedOrders)
				return;

			session.CreateSQLQuery("delete from Logs.PendingOrderLogs where UserId = :userId")
				.SetParameter("userId", user.Id)
				.ExecuteUpdate();

			var addresses = Addresses.Select(a => (uint?)a.Id).ToArray();
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

			Export(Result, "Orders",
				new[] {
					"ExportId",
					"CreatedOn",
					"AddressId",
					"PriceId",
					"RegionId",
					"Comment",
					"IsLoaded"
				},
				groups.Select(g => new object[] {
					g.First().RowId,
					g.First().WriteTime.ToUniversalTime(),
					g.Key.AddressId,
					g.Key.PriceList.PriceCode,
					g.Key.RegionCode,
					g.Where(o => !String.IsNullOrWhiteSpace(o.ClientAddition))
						.Implode(o => $"{o.UserId}: {o.ClientAddition}", " | "),
					true
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
	ifnull(oo.VitallyImportant, 0) as VitallyImportant,
	oo.RegistryCost,
	mx.Cost as MaxProducerCost,
	ol.RequestRatio,
	ol.OrderCost as MinOrderSum,
	ol.MinOrderCount,
	oo.ProducerCost,
	oo.NDS,
	ol.EAN13 as BarCode,
	ol.CodeOKP,
	ol.Series,
	st.Synonym as ProductSynonym,
	si.Synonym as ProducerSynonym,
	ol.Cost,
	oh.RegionCode as RegionId,
	ol.CoreId as OfferId,
	ol.Junk as OriginalJunk
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
			Export(Result, sql, "OrderLines", truncate: false, parameters: new { userId = user.Id });
		}

		private object GetAddressId(Address address, OrderBatchItem item)
		{
			var id = address.Id;
			if (item.Item == null)
				return id;
			if (item.Item.Address != null)
				id = item.Item.Address.Id;
			var orderItem = item.Item.OrderItems.FirstOrDefault();
			if (orderItem != null)
				id = orderItem.Order.AddressId.Value;
			return id;
		}

		public void ExportSentOrders(ulong[] existOrderIds)
		{
			var addresses = user.AvaliableAddresses.Where(a => a.Enabled).ToArray();
			if (addresses.Length == 0)
				return;

			CreateMaxProducerCosts();
			var condition = new StringBuilder("where oh.Deleted = 0");
			if (existOrderIds.Length > 0) {
				condition.Append(" and oh.RowId not in (");
				condition.Append(existOrderIds.Implode());
				condition.Append(") ");
			}
			condition.Append(" and oh.AddressId in (");
			condition.Append(addresses.Implode(a => a.Id));
			condition.Append(") ");

			var sql = $@"
select oh.RowId as ServerId,
	convert_tz(oh.WriteTime, @@session.time_zone,'+00:00') as CreatedOn,
	convert_tz(oh.WriteTime, @@session.time_zone,'+00:00') as SentOn,
	convert_tz(oh.PriceDate, @@session.time_zone,'+00:00') as PriceDate,
	oh.AddressId,
	oh.PriceCode as PriceId,
	oh.RegionCode as RegionId,
	oh.ClientAddition as Comment
from Orders.OrdersHead oh
{condition}";
			Export(Result, sql, "SentOrders", truncate: false, parameters: new { userId = user.Id });

			sql = $@"
select ol.RowId as ServerId,
	oh.RowId as ServerOrderId,
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
	ifnull(oo.VitallyImportant, 0) as VitallyImportant,
	oo.RegistryCost,
	mx.Cost as MaxProducerCost,
	ol.RequestRatio,
	ol.OrderCost as MinOrderSum,
	ol.MinOrderCount,
	oo.ProducerCost,
	oo.NDS,
	ol.EAN13 as BarCode,
	ol.CodeOKP,
	ol.Series,
	st.Synonym as ProductSynonym,
	si.Synonym as ProducerSynonym,
	ol.Cost,
	ifnull(ol.CostWithDelayOfPayment, ol.Cost) as ResultCost,
	ol.Junk as OriginalJunk
from Orders.OrdersHead oh
	join Orders.OrdersList ol on ol.OrderId = oh.RowId
		join Catalogs.Products p on p.Id = ol.ProductId
		left join farm.synonymArchive st on st.SynonymCode = ol.SynonymCode
		left join farm.synonymFirmCr si on si.SynonymFirmCrCode = ol.SynonymFirmCrCode
		left join Catalogs.Producers pr on pr.Id = ol.CodefirmCr
		left join Orders.OrderedOffers oo on oo.Id = ol.RowId
		left join Usersettings.MaxProducerCosts mx on mx.ProductId = ol.ProductId and mx.ProducerId = ol.CodeFirmCr
{condition}
group by ol.RowId";
			Export(Result, sql, "SentOrderLines", truncate: false, parameters: new { userId = user.Id });
		}

		public void ExportDocs()
		{
			string sql;

			session.CreateSQLQuery(@"delete from Logs.PendingDocLogs"
				+ " where UserId = :userId;")
				.SetParameter("userId", user.Id)
				.ExecuteUpdate();

			var logs = session.Query<DocumentSendLog>()
				.Where(l => !l.Committed && l.User.Id == user.Id)
				.OrderByDescending(x => x.Document.LogTime)
				.Take(1000)
				.ToArray();

			if (logs.Length == 0) {
				//мы должны передать LoadedDocuments что бы клиент очистил таблицу
				Export(Result,
					"LoadedDocuments",
					new[] { "Id", "Type", "SupplierId", "OriginFilename", "IsDocDelivered" },
					new object[0][]);
				return;
			}

			foreach (var log in logs) {
				log.Committed = false;
				log.FileDelivered = false;
				log.DocumentDelivered = false;
			}

			foreach (var doc in logs) {
				if (doc.Document.DocumentType == DocType.Waybills && !user.SendWaybills)
					continue;
				if (doc.Document.DocumentType == DocType.Rejects && !user.SendRejects)
					continue;
				//если это конвертированный документ мы не должны доставлять файл но должны доставить разобранную
				//накладную
				if (doc.Document.IsFake)
					continue;
				try {
					var type = doc.Document.DocumentType.ToString();
					var path = Path.Combine(DocsPath,
						doc.Document.AddressId.ToString(),
						type);
					if (!Directory.Exists(path)) {
						log.Warn($"Директория для загрузки документов не существует {path}");
						continue;
					}
					var files = Directory.GetFiles(path, String.Format("{0}_*", doc.Document.Id));
					Result.AddRange(files.Select(f => new UpdateData(Path.Combine(type, doc.GetTargetFilename(f))) {
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
			sql = String.Format(@"drop temporary table if exists RetailCostFixed;
create temporary table RetailCostFixed
(Id int(10) unsigned, index using hash (Id))
engine = memory
select dh.Id, IFNULL(SUM(db.RetailCost),0) > 0 as IsRetailCostFixed
from Documents.DocumentHeaders dh
left join Documents.DocumentBodies db on db.DocumentId = dh.Id
where dh.DownloadId in ({0})
group by dh.Id;

select d.RowId as Id,
	dh.ProviderDocumentId,
	convert_tz(now(), @@session.time_zone,'+00:00') as WriteTime,
	convert_tz(dh.DocumentDate, @@session.time_zone,'+00:00') as DocumentDate,
	dh.AddressId,
	dh.FirmCode as SupplierId,
	d.DocumentType as DocType,
	i.SellerName,
	i.SellerAddress,
	i.SellerInn,
	i.SellerKpp,
	i.BuyerName,
	i.BuyerAddress,
	i.BuyerInn,
	i.BuyerKpp,
	i.ConsigneeInfo as ConsigneeNameAndAddress,
	i.ShipperInfo as ShipperNameAndAddress,
	i.InvoiceNumber as InvoiceId,
	i.InvoiceDate,
	if(d.PreserveFilename, d.FileName, null) as Filename,
	rf.IsRetailCostFixed
from Logs.Document_logs d
	join Documents.DocumentHeaders dh on dh.DownloadId = d.RowId
	join RetailCostFixed rf on rf.Id = dh.Id
	left join Documents.InvoiceHeaders i on i.Id = dh.Id
where d.RowId in ({0})
group by dh.Id
union
select d.RowId as Id,
	d.RowId as ProviderDocumentId,
	convert_tz(now(), @@session.time_zone,'+00:00') as WriteTime,
	convert_tz(rh.WriteTime, @@session.time_zone,'+00:00') as DocumentDate,
	rh.AddressId,
	rh.SupplierId,
	d.DocumentType as DocType,
	null as SellerName,
	null as SellerAddress,
	null as SellerInn,
	null as SellerKpp,
	null as BuyerName,
	null as BuyerAddress,
	null as BuyerInn,
	null as BuyerKpp,
	null as ConsigneeNameAndAddress,
	null as ShipperNameAndAddress,
	null as InvoiceId,
	null as InvoiceDate,
	if(d.PreserveFilename, d.FileName, null) as Filename,
	0 as IsRetailCostFixed
from Logs.Document_logs d
	join Documents.RejectHeaders rh on rh.DownloadId = d.RowId
where d.RowId in ({0})", ids);
			Export(Result, sql, "Waybills", truncate: false, parameters: new { userId = user.Id });
			session.CreateSQLQuery(@"drop temporary table if exists RetailCostFixed;").ExecuteUpdate();

			Stock.CreateInTransitStocks(session, user);
			sql = $@"
select db.Id,
	d.RowId as WaybillId,
	db.ProductId,
	p.CatalogId,
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
	str_to_date(db.Period, '%d.%m.%Y') as Exp,
	db.Certificates,
	db.EAN13,
	db.CountryCode,
	db.RetailCost as ServerRetailCost,
	db.RetailCostMarkup as ServerRetailMarkup,
	s.Id as StockId,
	s.Version as StockVersion
from Logs.Document_logs d
		join Documents.DocumentHeaders dh on dh.DownloadId = d.RowId
			join Documents.DocumentBodies db on db.DocumentId = dh.Id
				left join Catalogs.Products p on p.Id = db.ProductId
				left join Inventory.Stocks s on s.WaybillLineId = db.Id
where d.RowId in ({ids})
group by dh.Id, db.Id";
			Export(Result, sql, "WaybillLines", truncate: false, parameters: new { userId = user.Id });

			sql = $@"
select DocumentLineId, OrderLineId
from Documents.WaybillOrders wo
	join Documents.DocumentBodies db on db.Id = wo.DocumentLineId
		join Documents.DocumentHeaders dh on db.DocumentId = dh.Id
			join Logs.Document_logs d on dh.DownloadId = d.RowId
where d.RowId in ({ids})";
			Export(Result, sql, "WaybillOrders", truncate: false);
			ExportStocks(data.LastUpdateAt);

			var documentExported = session.CreateSQLQuery(@"
select dh.DownloadId
from Documents.DocumentHeaders dh
where dh.DownloadId in (:ids)")
				.SetParameterList("ids", logs.Select(d => d.Document.Id).ToArray())
				.List<uint>();
			logs.Where(l => documentExported.Contains(l.Document.Id))
				.Each(l => l.DocumentDelivered = true);

			sql = $@"
select Id, convert_tz(WriteTime, @@session.time_zone,'+00:00') as WriteTime, DownloadId, SupplierId
from Documents.RejectHeaders r
where r.DownloadId in ({ids})";
			Export(Result, sql, "OrderRejects", truncate: false, parameters: new { userId = user.Id });

			sql = $@"
select l.Id,
	l.Product,
	l.ProductId,
	p.CatalogId,
	l.Code,
	l.Producer,
	l.ProducerId,
	l.Rejected as Count,
	l.HeaderId as OrderRejectId
from Documents.RejectHeaders r
	join Documents.RejectLines l on l.Headerid = r.Id
		left join Catalogs.Products p on p.Id = l.ProductId
where r.DownloadId in ({ids})";
			Export(Result, sql, "OrderRejectLines", truncate: false, parameters: new { userId = user.Id });

			documentExported = session.CreateSQLQuery(@"
select r.DownloadId
from Documents.RejectHeaders r
where r.DownloadId in (:ids)")
				.SetParameterList("ids", logs.Select(d => d.Document.Id).ToArray())
				.List<uint>();
			logs.Where(l => documentExported.Contains(l.Document.Id))
				.Each(l => l.DocumentDelivered = true);

			var delivered = logs.Where(l => l.DocumentDelivered || l.FileDelivered).ToArray();
			Export(Result,
				"LoadedDocuments",
				new[] { "Id", "Type", "SupplierId", "OriginFilename", "IsDocDelivered" },
				delivered.Select(l => new object[] {
					l.Document.Id,
					(int)l.Document.DocumentType,
					l.Document.Supplier.Id,
					l.FileDelivered ? l.Document.Filename : null,
					l.DocumentDelivered
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
			using(var file = new StreamWriter(File.Create(filename), Encoding.GetEncoding(1251))) {
				MySqlHelper.Export(exportData, file);
			}
		}

		public void Export(List<UpdateData> data, string sql, string file, bool truncate, object parameters = null)
		{
			var dataAdapter = new MySqlDataAdapter(sql + " limit 0", (MySqlConnection)session.Connection);
			var dictionary = parameters != null ? ObjectExtentions.ToDictionary(parameters) : new Dictionary<string, object>();
			dictionary.Each(k => dataAdapter.SelectCommand.Parameters.AddWithValue(k.Key, k.Value));

			var table = new DataTable();
			dataAdapter.Fill(table);
			var columns = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();

			var path = Path.GetFullPath(Path.Combine(Config.RemoteExportPath, Prefix + file + ".txt"));
			var mysqlPath = path.Replace(@"\", "/");
			File.Delete(mysqlPath);
			sql += " INTO OUTFILE '" + mysqlPath + "' ";
			var command = new MySqlCommand(sql, (MySqlConnection)session.Connection);
			dictionary.Each(k => command.Parameters.AddWithValue(k.Key, k.Value));

			log.DebugFormat("Запрос {0}{1}", sql,
				dictionary.Count > 0 ? "\r\nпараметры: " + dictionary.Implode(k => $"{k.Key} = {k.Value}") : "");
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

		public string Compress(string file)
		{
			file = Path.GetFullPath(Path.Combine(ResultPath, file));
			using(var stream = File.Create(file)) {
				var watch = new Stopwatch();
				watch.Start();
				Compress(stream);
				watch.Stop();
				log.DebugFormat("Архив {2} создан за {0}, размер {1}", watch.Elapsed, stream.Length, file);
			}
			job.Size = new FileInfo(file).Length;
			return file;
		}

		public void Compress(Stream stream)
		{
			if (!String.IsNullOrEmpty(Config.InjectedFault))
				throw new Exception(Config.InjectedFault);

			using (var zip = ZipFile.Create(stream)) {
				((ZipEntryFactory)zip.EntryFactory).IsUnicodeText = true;

				zip.BeginUpdate();
				foreach (var tuple in Result) {
					var filename = tuple.LocalFileName;
					//экспортировать пустые файлы важно тк пустой файл приведет к тому что таблица будет очищена
					//например в случае если последний адрес доставки был отключен
					if (String.IsNullOrEmpty(filename)) {
						var content = new MemoryDataSource(new MemoryStream(Encoding.UTF8.GetBytes(tuple.Content)));
						zip.Add(content, tuple.ArchiveFileName);
					}
					else {
						if (Config.ExportTimeout != TimeSpan.Zero)
							WaitHelper.WaitOrFail(Config.ExportTimeout,
								() => File.Exists(filename),
								$"Не найден файл для экспорта {filename}");

						if (File.Exists(filename)) {
							//будь бдителен если в текущей директории существует файл с именем как tuple.ArchiveFileName то будет заархивирован он а не
							//filename System.Exception : ICSharpCode.SharpZipLib.Zip.ZipException: Entry size/stream size mismatch
							if (File.Exists(tuple.ArchiveFileName))
								throw new Exception($"Файл {Path.GetFullPath(tuple.ArchiveFileName)} существует это" +
									$" приведет к ошибке Entry size/stream size mismatch, удали и повтори попытку.");
							zip.Add(filename, tuple.ArchiveFileName);
						}
						else {
#if DEBUG
							throw new Exception($"Не найден файл для экспорта {filename}");
#else
							log.WarnFormat("Не найден файл для экспорта {0}", filename);
#endif
						}
					}
				}
				zip.CommitUpdate();
			}

			foreach (var raw in External.Where(x => String.IsNullOrEmpty(x.Filename))) {
				var key = "ext-" + Path.GetFileName(raw.Dir) + ".zip";
				var cacheFile = Path.Combine(Config.CachePath, key);
				var files = new DirectoryInfo(raw.Dir).EnumerateFiles();
				if (!files.Any())
					continue;
				if (new FileInfo(cacheFile).LastWriteTime > files.Select(x => x.LastWriteTime).Max()) {
					raw.Filename = cacheFile;
					continue;
				}

				var tmp = cleaner.TmpFile();
				using (var zip = ZipFile.Create(tmp)) {
					((ZipEntryFactory)zip.EntryFactory).IsUnicodeText = true;
					zip.BeginUpdate();
					var dir = raw.Dir;
					var name = raw.Name;
					var transform = new ZipNameTransform();
					transform.TrimPrefix = dir;

					var scanned = new FileSystemScanner(".+");
					scanned.ProcessFile += (sender, args) => {
						if (new FileInfo(args.Name).Attributes.HasFlag(FileAttributes.Hidden))
							return;
						if (name != "")
							name = name + "/";
						zip.Add(args.Name, name + transform.TransformFile(args.Name));
					};
					scanned.Scan(dir, true);
					zip.CommitUpdate();
				}
				File.Delete(cacheFile);
				File.Move(tmp, cacheFile);
				raw.Filename = cacheFile;
			}

			//на разных серверах абсолютные пути могут отличаться но относительные будут совпадать
			var multiparts = External.Select(x => FileHelper.RelativeTo(x.Filename, Path.GetFullPath(FileHelper.MakeRooted(@".\")))).ToArray();
			if (multiparts.Length > 0)
				job.MultipartContent = JsonConvert.SerializeObject(multiparts);
		}

		public void ExportDb()
		{
			Export();
			ExportAds();
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

		public void ExportAds()
		{
			if (!clientSettings.ShowAdvertising) {
				//нужно подать сигнал клиенту что он должен очистить папку с рекламой
				Result.Add(new UpdateData("ads/delete.me") { Content = "" });
				return;
			}
			if (!Directory.Exists(AdsPath)) {
				log.WarnFormat("Директория рекламы не найдена '{0}'", AdsPath);
				return;
			}
			var template = $"_{user.Client.RegionCode}";
			var dir = Directory.GetDirectories(AdsPath).FirstOrDefault(d => d.EndsWith(template));
			if (String.IsNullOrEmpty(dir)) {
				log.WarnFormat("Директория рекламы не найдена по маске '{0}' в '{1}'", template, AdsPath);
				return;
			}

			var files = new DirectoryInfo(dir).EnumerateFiles()
				.Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden)
					&& RoundToSeconds(f.LastWriteTime) > data.LastReclameUpdateAt.GetValueOrDefault()
					&& f.Length < Config.MaxReclameFileSize)
				.ToArray();
			if (files.Length == 0) {
				data.LastPendingReclameUpdateAt = null;
				return;
			}
			data.LastPendingReclameUpdateAt = files.Max(x => x.LastWriteTime);
			//мы должны экспортировать все файлы
			files = new DirectoryInfo(dir).EnumerateFiles()
				.Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden) && f.Length < Config.MaxReclameFileSize)
				.ToArray();
			Result.AddRange(files.Select(f => new UpdateData("ads/" + f.Name) { LocalFileName = f.FullName }));
		}

		//mysql хранит даты с точностью до секунды и если мы сравниваем дату из базы с датой из другого источника
		//например файловой системы, перед сравнением ее нужно округлить
		public static DateTime RoundToSeconds(DateTime value)
		{
			return new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Kind);
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

		public bool ExportBin()
		{
			var updateDir = Config.GetUpdatePath(data, job);
			var file = Path.Combine(updateDir, "version.txt");
			if (!File.Exists(file)) {
				log.DebugFormat("Не найден файл версии {0}", Path.GetFullPath(file));
				return false;
			}

			var updateVersion = Version.Parse(File.ReadAllText(file));
			if (updateVersion <= job.Version)
				return false;

			if (!String.IsNullOrEmpty(job.ErrorDescription))
				job.ErrorDescription += Environment.NewLine;
			job.ErrorDescription += $"Обновление включает новую версию приложения {updateVersion}" +
				$" из канала {Path.GetFileName(updateDir)}";

			var deltaUpdate = Path.Combine(Config.UpdatePath, $"delta-{job.Version}-{updateVersion}.zip");
			if (File.Exists(deltaUpdate))
				External.Add(ExternalRawFile.FromFile(deltaUpdate));
			else
				External.Add(ExternalRawFile.FromDir("update", updateDir));

			var perUserUpdate = Path.Combine(Config.PerUserUpdatePath, user.Id.ToString());
			if (File.Exists(perUserUpdate))
				AddDir(Result, perUserUpdate, "update");
			return true;
		}

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			cleaner.Dispose();
		}

		public void Confirm(ConfirmRequest request)
		{
			job.Confirm(Config, request.Message);
			data.Confirm();
			var userId = job.User.Id;

			//каждый запрос выполняется отдельно что бы проще было диагностировать блокировки
			session.CreateSQLQuery(@"
update Usersettings.AnalitFReplicationInfo r
set r.ForceReplication = 0
where r.UserId = :userId and r.ForceReplication = 2;")
				.SetParameter("userId", userId)
				.ExecuteUpdate();

			session.CreateSQLQuery(@"
update Logs.DocumentSendLogs l
	join Logs.PendingDocLogs p on p.SendLogId = l.Id
set l.Committed = 1, l.SendDate = now()
where p.UserId = :userId;")
				.SetParameter("userId", userId)
				.ExecuteUpdate();

			session.CreateSQLQuery(@"
delete from Logs.PendingDocLogs
where UserId = :userId;")
				.SetParameter("userId", userId).ExecuteUpdate();

			session.CreateSQLQuery(@"
update Logs.MailSendLogs l
	join Logs.PendingMailLogs p on p.SendLogId = l.Id
set l.Committed = 1
where p.UserId = :userId;")
				.SetParameter("userId", userId)
				.ExecuteUpdate();

			session.CreateSQLQuery(@"
delete from Logs.PendingMailLogs
where UserId = :userId;")
				.SetParameter("userId", userId)
				.ExecuteUpdate();

			session.CreateSQLQuery(@"
update Orders.OrdersHead oh
	join Logs.PendingOrderLogs l on l.OrderId = oh.RowId
set oh.Deleted = 1
where l.UserId = :userId;")
				.SetParameter("userId", userId)
				.ExecuteUpdate();

			session.CreateSQLQuery(@"
delete from Logs.PendingOrderLogs
where UserId = :userId;")
				.SetParameter("userId", userId)
				.ExecuteUpdate();
		}

		public void ExportStocks(DateTime lastSync)
		{
			var sql = @"
select if(s.CreatedByUserId = ?userId, s.ClientPrimaryKey, null) as Id,
	s.Id as ServerId,
	s.Version as ServerVersion,
	s.AddressId,
	s.ProductId,
	s.CatalogId,
	s.Product,
	s.ProducerId,
	s.Producer,
	s.Country,
	s.Period,
	s.Exp,
	s.SerialNumber,
	s.Certificates,
	s.Unit,
	s.ExciseTax,
	s.BillOfEntryNumber,
	s.VitallyImportant,
	s.ProducerCost,
	s.RegistryCost,
	s.SupplierPriceMarkup,
	s.SupplierCostWithoutNds,
	s.SupplierCost,
	s.Nds,
	s.NdsAmount,
	s.Barcode,
	s.Status,
	s.Quantity,
	s.SupplyQuantity,
	s.RetailCost,
	s.RetailMarkup,
	dh.DownloadId as WaybillId
from Inventory.Stocks s
	join Customers.Addresses a on a.Id = s.AddressId
		join Customers.UserAddresses ua on ua.Addressid = a.Id
			join Customers.Users u on u.Id = ua.UserId
	left join Documents.DocumentBodies db on db.Id = s.WaybillLineId
		left join Documents.DocumentHeaders dh on dh.Id = db.DocumentId
where a.Enabled = 1
	and u.Id = ?userId
	and s.Timestamp > ?lastSync";
			Export(Result, sql, "stocks", false, new {userId = user.Id, lastSync});
		}
	}

	public static class DbRecordHelper
	{
		public static uint? GetNullableUInt32(this DbDataRecord record, string column)
		{
			var value = record[column];
			if (value is DBNull)
				return null;
			if (value is uint)
				return (uint)value;
			return Convert.ToUInt32(value);
		}

		public static decimal? GetNullableDecimal(this DbDataRecord record, string column)
		{
			var value = record[column];
			if (value is DBNull)
				return null;
			if (value is decimal)
				return (decimal)value;
			return Convert.ToDecimal(value);
		}
	}
}

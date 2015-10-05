using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Orders;
using Common.NHibernate;
using Common.Tools;
using Diadoc.Api;
using Diadoc.Api.Cryptography;
using Diadoc.Api.Proto.Documents;
using Diadoc.Api.Proto.Documents.NonformalizedDocument;
using Diadoc.Api.Proto.Events;
using Ionic.Zip;
using log4net;
using log4net.Config;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Linq;
using AggregateException = System.AggregateException;

namespace AnalitF.Net.Client.Models.Commands
{
#if DEBUG
	public class DebugServerError
	{
		public string Message;
		public string ExceptionMessage;
		public string StackTrace;

		public override string ToString()
		{
			return new StringBuilder()
				.AppendLine(ExceptionMessage)
				.AppendLine(StackTrace)
				.ToString();
		}
	}
#endif

	public class UpdateCommand : RemoteCommand
	{
		public bool Clean = true;

		public uint AddressId;
		public string BatchFile;

		private string syncData = "";
		private UpdateResult result = UpdateResult.OK;
		private uint requestId;

		public UpdateCommand()
		{
			ErrorMessage = "Не удалось получить обновление. Попробуйте повторить операцию позднее.";
			SuccessMessage = "Обновление завершено успешно.";
		}

		public string SyncData
		{
			get { return syncData; }
			set
			{
				syncData = value ?? "";
				if (syncData.Match("Waybills")) {
					SuccessMessage = "Получение документов завершено успешно.";
				}
				else {
					SuccessMessage = "Обновление завершено успешно.";
				}
			}
		}

		protected override UpdateResult Execute()
		{
			Reporter.StageCount(4);

			var sendLogsTask = SendLogs(Client, Token);
			HttpResponseMessage response;
			var updateType = "накопительное";
			var user = Session.Query<User>().FirstOrDefault();
			var settings = Session.Query<Settings>().First();
			if (SyncData.Match("Batch")) {
				updateType = "автозаказ";
				SuccessMessage = "Автоматическая обработка дефектуры завершена.";
				response = Wait("Batch", Client.PostAsync("Batch", GetBatchRequest(user, settings), Token), ref requestId);
			}
			else if (SyncData.Match("WaybillHistory")) {
				SuccessMessage = "Загрузка истории документов завершена успешно.";
				updateType = "загрузка истории заказов";
				var data = new HistoryRequest {
					WaybillIds = Session.Query<Waybill>().Select(w => w.Id).ToArray(),
					IgnoreOrders = true,
				};
				response = Wait("History", Client.PostAsJsonAsync("History", data, Token), ref requestId);
			}
			else if (SyncData.Match("OrderHistory")) {
				SuccessMessage = "Загрузка истории заказов завершена успешно.";
				updateType = "загрузка истории заказов";
				var data = new HistoryRequest {
					OrderIds = Session.Query<SentOrder>().Select(o => o.ServerId).ToArray(),
					IgnoreWaybills = true,
				};
				response = Wait("History", Client.PostAsJsonAsync("History", data, Token), ref requestId);
			}
			else {
				var lastSync = user?.LastSync;
				if (lastSync == null) {
					updateType = "куммулятивное";
				}
				if (syncData.Match("Waybills")) {
					updateType = "загрузка накладных";
				}
				var url = Config.SyncUrl(syncData, lastSync);
				SendPrices(Client, settings, Token);
				var request = Client.GetAsync(url, Token);
				response = Wait(Config.WaitUrl(url, syncData).ToString(), request, ref requestId);
			}

			Reporter.Stage("Загрузка данных");
			Log.InfoFormat("Запрос обновления, тип обновления '{0}'", updateType);
			Download(response, Config.ArchiveFile, Reporter);
			Log.InfoFormat("Обновление загружено, размер {0}", new FileInfo(Config.ArchiveFile).Length);
			try {
				Diadok();
			}
			catch(WebException e) {
				Log.Error("Ошибка при обращении к Диадок", e);
				var http = e.Response as HttpWebResponse;
				if (http != null && (int)http.StatusCode == 401) {
					Results.Add(MessageResult.Error("Не удалось получить документы Диадок. Введены некорректные учетные данные."));
				}
				else {
					Results.Add(MessageResult.Error("Не удалось получить документы Диадок. Попробуйте повторить операцию позднее."));
				}
			}
			catch(Exception e) {
				Log.Error("Ошибка при обращении к Диадок", e);
				Results.Add(MessageResult.Error("Не удалось получить документы Диадок. Попробуйте повторить операцию позднее."));
			}

			var result = ProcessUpdate(Config.ArchiveFile);
			Log.InfoFormat("Обновление завершено успешно");

			WaitAndLog(sendLogsTask, "Отправка логов");
			return result;
		}

		private HttpContent GetBatchRequest(User user, Settings settings)
		{
			var request = new BatchRequest(AddressId, settings.JunkPeriod, user?.LastSync);
			if (String.IsNullOrEmpty(BatchFile)) {
				request.BatchItems = StatelessSession.Query<BatchLine>().ToArray().Select(l => new BatchItem {
					Code = l.Code,
					CodeCr = l.CodeCr,
					ProductName = l.ProductSynonym,
					ProducerName = l.ProducerSynonym,
					Quantity = l.Quantity,
					SupplierDeliveryId = l.SupplierDeliveryId,
					ServiceValues = l.ParsedServiceFields,
					Priority = l.Priority,
					BaseCost = l.BaseCost
				}).ToList();
				return new ObjectContent<BatchRequest>(request, Formatter);
			}
			else {
				try {
					var tmp = FileHelper.SelfDeleteTmpFile();
					using (var zip = new ZipFile()) {
						zip.AddFile(BatchFile).FileName = "payload";
						zip.AddEntry("meta.json", JsonConvert.SerializeObject(request));
						zip.Save(tmp);
					}
					tmp.Position = 0;
					return new StreamContent(tmp);
				}
				//транслируем сообщения об ошибках, если кто то зажал или удалил файл автозаказа
				catch(IOException e) {
					throw new EndUserError(ErrorHelper.TranslateIO(e));
				}
				catch(UnauthorizedAccessException e) {
					throw new EndUserError(e.Message);
				}
			}
		}

		public UpdateResult ProcessUpdate(string file)
		{
			if (Directory.Exists(Config.UpdateTmpDir))
				Directory.Delete(Config.UpdateTmpDir, true);

			Directory.CreateDirectory(Config.UpdateTmpDir);

			using (var zip = new ZipFile(file)) {
				zip.ExtractAll(Config.UpdateTmpDir, ExtractExistingFileAction.OverwriteSilently);
			}

			if (File.Exists(Path.Combine(Config.UpdateTmpDir, "update", "Updater.exe"))) {
				Log.InfoFormat("Получено обновление приложения");
				return UpdateResult.UpdatePending;
			}

			Import();
			return result;
		}


		//миграция данных из delphi приложения
		public void Migrate()
		{
			var filename = FileHelper.MakeRooted("Params.txt");
			if (File.Exists(filename)) {
				var mysqlFilename = Path.GetFullPath(filename).Replace("\\", "/");
				Session.CreateSQLQuery($@"
drop temporary table if exists temp_params;
create temporary table temp_params (
	Id int unsigned not null auto_increment,
	UseRas tinyint(1) not null,
	RasConnection varchar(255),
	UserName varchar(255),
	Password varchar(255),
	UseProxy  tinyint(1) not null,
	ProxyHost varchar(255),
	ProxyPort int,
	ProxyUserName varchar(255),
	ProxyPassword varchar(255),
	DeleteOrdersOlderThan int,
	ConfirmDeleteOldOrders tinyint(1) not null,
	OpenWaybills tinyint(1) not null,
	OpenRejects tinyint(1) not null,
	PrintOrdersAfterSend tinyint(1) not null,
	ConfirmSendOrders tinyint(1) not null,
	CanViewOffersByCatalogName tinyint(1) not null,
	GroupByProduct tinyint(1) not null,
	primary key(Id)
);

load data infile '{mysqlFilename}' replace into table temp_params
(UseRas, RasConnection, UserName, Password, UseProxy, ProxyHost, ProxyPort,
ProxyUserName, ProxyPassword, DeleteOrdersOlderThan, ConfirmDeleteOldOrders, OpenWaybills,
OpenRejects, PrintOrdersAfterSend, ConfirmSendOrders, CanViewOffersByCatalogName, GroupByProduct);

update (Settings s, temp_params t)
set s.UseRas = t.UseRas,
	s.RasConnection = t.RasConnection,
	s.UserName = t.UserName,
	s.UseProxy = t.UseProxy,
	s.ProxyHost = t.ProxyHost,
	s.ProxyPort = t.ProxyPort,
	s.ProxyUserName = t.ProxyUserName,
	s.ProxyPassword = t.ProxyPassword,
	s.DeleteOrdersOlderThan = t.DeleteOrdersOlderThan,
	s.ConfirmDeleteOldOrders = t.ConfirmDeleteOldOrders,
	s.OpenWaybills = t.OpenWaybills,
	s.OpenRejects = t.OpenRejects,
	s.PrintOrdersAfterSend = t.PrintOrdersAfterSend,
	s.ConfirmSendOrders = t.ConfirmSendOrders,
	s.CanViewOffersByCatalogName = t.CanViewOffersByCatalogName,
	s.GroupByProduct = t.GroupByProduct;

drop temporary table if exists temp_params;")
					.ExecuteUpdate();
				File.Delete(filename);
			}

			filename = FileHelper.MakeRooted("RetailMargins.txt");
			if (File.Exists(filename)) {
				var mysqlFilename = Path.GetFullPath(filename).Replace("\\", "/");
				Session.CreateSQLQuery($@"
delete from MarkupConfigs
where Type = 0;

load data infile '{mysqlFilename}' replace into table MarkupConfigs(Begin, End, Markup, MaxMarkup, MaxSupplierMarkup);

update MarkupConfigs
set Type = 0
where SettingsId is null;

update MarkupConfigs
set SettingsId = (select Id from Settings limit 1)
where SettingsId is null;")
					.ExecuteUpdate();
				File.Delete(filename);
			}

			filename = FileHelper.MakeRooted("VitallyImportantMarkups.txt");
			if (File.Exists(filename)) {
				var mysqlFilename = Path.GetFullPath(filename).Replace("\\", "/");
				Session.CreateSQLQuery($@"
delete from MarkupConfigs
where Type = 1;

load data infile '{mysqlFilename}' replace into table MarkupConfigs(Begin, End, Markup, MaxMarkup, MaxSupplierMarkup);

update MarkupConfigs
set Type = 1
where SettingsId is null;

update MarkupConfigs
set SettingsId = (select Id from Settings limit 1)
where SettingsId is null;")
					.ExecuteUpdate();
				File.Delete(filename);
			}

			filename = FileHelper.MakeRooted("Password.txt");
			if (File.Exists(filename)) {
				var password = File.ReadAllText(filename);
				Session.CreateSQLQuery("update Settings set Password = :password")
					.SetParameter("password", password)
					.ExecuteUpdate();
				File.Delete(filename);
			}

			filename = FileHelper.MakeRooted("GlobalParams.txt");
			if (File.Exists(filename)) {
				var mysqlFilename = Path.GetFullPath(filename).Replace("\\", "/");
				Session.CreateSQLQuery($@"
drop temporary table if exists temp_global_params;
create temporary table temp_global_params (
	Id int unsigned not null auto_increment,
	`Key` varchar(255),
	`Value` varchar(255),
	primary key(Id)
);

load data infile '{mysqlFilename}' replace into table temp_global_params (`Key`, `Value`);

update (Settings s, temp_global_params t)
set s.GroupWaybillsBySupplier = t.Value
where t.`Key` = 'GroupWaybillsBySupplier';

update (Settings s, temp_global_params t)
set s.DeleteWaybillsOlderThan = t.Value
where t.`Key` = 'WaybillsHistoryDayCount';

update (Settings s, temp_global_params t)
set s.ConfirmDeleteOldWaybills = t.Value
where t.`Key` = 'ConfirmDeleteOldWaybills';

update (Settings s, temp_global_params t)
set s.MaxOverCostOnRestoreOrder = t.Value
where t.`Key` = 'NetworkPositionPercent';

update (Settings s, temp_global_params t)
set s.BaseFromCategory = t.Value
where t.`Key` = 'BaseFirmCategory';

update (Settings s, temp_global_params t)
set s.OverCostWarningPercent = t.Value
where t.`Key` = 'Excess';

update (Settings s, temp_global_params t)
set s.OverCountWarningFactor = t.Value
where t.`Key` = 'ExcessAvgOrderTimes';

update (Settings s, temp_global_params t)
set s.DiffCalcMode = t.Value
where t.`Key` = 'DeltaMode';

update (Settings s, temp_global_params t)
set s.ShowPriceName = t.Value
where t.`Key` = 'ShowPriceName';

update (Settings s, temp_global_params t)
set s.HighlightUnmatchedOrderLines = t.Value
where t.`Key` = 'UseColorOnWaybillOrders';

update (Settings s, temp_global_params t)
set s.TrackRejectChangedDays = t.Value
where t.`Key` = 'NewRejectsDayCount';


update (Settings s, temp_global_params t)
set s.RackingMapPrintProduct = t.Value
where t.`Key` = 'RackCardReportProductVisible';

update (Settings s, temp_global_params t)
set s.RackingMapPrintProducer = t.Value
where t.`Key` = 'RackCardReportProducerVisible';

update (Settings s, temp_global_params t)
set s.RackingMapPrintSerialNumber = t.Value
where t.`Key` = 'RackCardReportSerialNumberVisible';

update (Settings s, temp_global_params t)
set s.RackingMapPrintPeriod = t.Value
where t.`Key` = 'RackCardReportPeriodVisible';

update (Settings s, temp_global_params t)
set s.RackingMapPrintQuantity = t.Value
where t.`Key` = 'RackCardReportQuantityVisible';

update (Settings s, temp_global_params t)
set s.RackingMapPrintSupplier = t.Value
where t.`Key` = 'RackCardReportProviderVisible';

update (Settings s, temp_global_params t)
set s.RackingMapPrintRetailCost = t.Value
where t.`Key` = 'RackCardReportCostVisible';

update (Settings s, temp_global_params t)
set s.RackingMapPrintCertificates = t.Value
where t.`Key` = 'RackCardReportCertificatesVisible';

update (Settings s, temp_global_params t)
set s.RackingMapPrintDocumentDate = t.Value
where t.`Key` = 'RackCardReportDateOfReceiptVisible';

update (Settings s, temp_global_params t)
set s.RackingMapHideNotPrinted = t.Value
where t.`Key` = 'RackCardReportDeleteUnprintableElemnts';

update (Settings s, temp_global_params t)
set s.RackingMapSize = t.Value
where t.`Key` = 'RackCardReportRackCardSize';

update (Settings s, temp_global_params t)
set s.PriceTagPrintEmpty = t.Value
where t.`Key` = 'TicketReportPrintEmptyTickets';

update (Settings s, temp_global_params t)
set s.PriceTagPrintFullName = t.Value
where t.`Key` = 'TicketReportClientNameVisible';

update (Settings s, temp_global_params t)
set s.PriceTagPrintProduct = t.Value
where t.`Key` = 'TicketReportProductVisible';

update (Settings s, temp_global_params t)
set s.PriceTagPrintCountry = t.Value
where t.`Key` = 'TicketReportCountryVisible';

update (Settings s, temp_global_params t)
set s.PriceTagPrintProducer = t.Value
where t.`Key` = 'TicketReportProducerVisible';

update (Settings s, temp_global_params t)
set s.PriceTagPrintPeriod = t.Value
where t.`Key` = 'TicketReportPeriodVisible';

update (Settings s, temp_global_params t)
set s.PriceTagPrintProviderDocumentId = t.Value
where t.`Key` = 'TicketReportProviderDocumentIdVisible';

update (Settings s, temp_global_params t)
set s.PriceTagPrintSupplier = t.Value
where t.`Key` = 'TicketReportSignatureVisible';

update (Settings s, temp_global_params t)
set s.PriceTagPrintSerialNumber = t.Value
where t.`Key` = 'TicketReportSerialNumberVisible';

update (Settings s, temp_global_params t)
set s.PriceTagPrintDocumentDate = t.Value
where t.`Key` = 'TicketReportDocumentDateVisible';

update (Settings s, temp_global_params t)
set s.PriceTagHideNotPrinted = t.Value
where t.`Key` = 'TicketReportDeleteUnprintableElemnts';

update (Settings s, temp_global_params t)
set s.PriceTagType = t.Value
where t.`Key` = 'TicketReportTicketSize';")
					.ExecuteUpdate();

				var colors = Session.CreateSQLQuery("select `Key`, `Value` from temp_global_params where `Key` like 'ln%'")
					.List<object[]>();

				//мы не можем обращаться к DefaultStyles тк они уже знают о dispatcher и при обращении бросят ошибку
				var defaultStyles = StyleHelper.GetDefaultStyles(StyleHelper.BuildDefaultStyles());
				var styleMap = new Dictionary<string, string> {
					{ "lnFrozenOrder", "Frozen" },
					{ "lnImportantMail", "" },
					{ "lnVitallyImportant", "VitallyImportant" },
					{ "lnRejectedColor", "IsReject" },
					{ "lnNeedCorrect", "IsOrderLineSendError" },
					{ "lnSmartOrderOptimalCost", "IsMinCost" },
					{ "lnModifiedWaybillByReject", "IsRejectChanged" },
					{ "lnCreatedByUserWaybill", "IsCreatedByUser" },
					{ "lnNotSetNDS", "IsNdsInvalid" },
					{ "lnSmartOrderAnotherError", "IsNotOrdered" },
					{ "lnMinReq", "IsInvalid" },
					{ "lnNonMain", "NotBase" },
					{ "lnMatchWaybill", "IsUnmatchedByWaybill" },
					{ "lnRejectedWaybillPosition", "IsRejectNew" },
					{ "lnUnrejectedWaybillPosition", "IsRejectCanceled" },
					{ "lnNewLetter", "" },
					{ "lnAwait", "" },
					{ "lnLeader", "Leader" },
					{ "lnBuyingBan", "IsForbidden" },
					{ "lnOrderedLikeFrozen", "ExistsInFreezed" },
					{ "lnRetailMarkup", "" },
					{ "lnRetailPrice", "" },
					{ "lnCertificateNotFound", "IsCertificateNotFound" },
					{ "lnSupplierPriceMarkup", "IsMarkupToBig" },
					{ "lnJunk", "Junk" },
				};
				foreach (var color in colors) {
					var styleName = styleMap[Convert.ToString(color[0])];
					if (string.IsNullOrEmpty(styleName))
						continue;
					var style = defaultStyles.FirstOrDefault(c => c.Name == styleName);

					var colorBytes = BitConverter.GetBytes(Convert.ToInt32(color[1]));
					//если это цвет начинается с ff то он из системной палитры а значит нужно использовать значение по умолчанию
					if (colorBytes[3] == 0xFF) {
						continue;
					}
					var importColor = Color.FromRgb(colorBytes[0], colorBytes[1], colorBytes[2]);
					if (style.Color != importColor)
					{
						var dbStyle = Session.Query<CustomStyle>().FirstOrDefault(x => x.Name == style.Name)
							?? style;
						dbStyle.Color = importColor;
						Session.Save(dbStyle);
					}
				}

				Session.CreateSQLQuery("drop temporary table if exists temp_global_params;").ExecuteUpdate();

				File.Delete(filename);
			}

			filename = FileHelper.MakeRooted("ClientSettings.txt");
			if (File.Exists(filename)) {
				var mysqlFilename = Path.GetFullPath(filename).Replace("\\", "/");
				Session.CreateSQLQuery($@"
drop temporary table if exists temp_client_settings;
create temporary table temp_client_settings (
	Id int unsigned not null auto_increment,
	AddressId int unsigned,
	Name varchar(255),
	Address varchar(255),
	Director varchar(255),
	Accountant varchar(255),
	Taxation int not null,
	IncludeNds tinyint(1) not null,
	IncludeNdsForVitallyImportant tinyint(1) not null,
	primary key(Id)
);

load data infile '{mysqlFilename}' replace into table temp_client_settings (AddressId, Name, Address, Director, Accountant,
	Taxation, IncludeNds, IncludeNdsForVitallyImportant);

update WaybillSettings s
join temp_client_settings t on t.AddressId = s.BelongsToAddressId
set s.Name = t.Name,
	s.Address = t.Address,
	s.Director = t.Director,
	s.Accountant = t.Accountant,
	s.Taxation = t.Taxation,
	s.IncludeNds = t.IncludeNds,
	s.IncludeNdsForVitallyImportant = t.IncludeNdsForVitallyImportant;

drop temporary table if exists temp_client_settings;")
					.ExecuteUpdate();
				File.Delete(filename);
			}

			filename = FileHelper.MakeRooted("ProviderSettings.txt");
			if (File.Exists(filename)) {
				var mysqlFilename = Path.GetFullPath(filename).Replace("\\", "/");
				Session.CreateSQLQuery($@"
drop temporary table if exists temp_provider_settings;
create temporary table temp_provider_settings (
	Id int unsigned not null auto_increment,
	SupplierId int unsigned,
	Dir varchar(255),
	primary key(Id)
);

load data infile '{mysqlFilename}' replace into table temp_provider_settings (SupplierId, Dir);

update DirMaps s
join temp_provider_settings t on t.SupplierId = s.SupplierId
set s.Dir = t.Dir;

drop temporary table if exists temp_provider_settings;")
					.ExecuteUpdate();
				File.Delete(filename);
			}

			filename = FileHelper.MakeRooted("Orders.txt");
			if (File.Exists(filename)) {
				var mysqlFilename = Path.GetFullPath(filename).Replace("\\", "/");
				Session.CreateSQLQuery($@"
load data infile '{mysqlFilename}' replace into table Orders
(Id, AddressId, PriceId, RegionId, CreatedOn, PersonalComment, Comment, Frozen);")
					.ExecuteUpdate();
				File.Delete(filename);
			}

			filename = FileHelper.MakeRooted("Lines.txt");
			if (File.Exists(filename)) {
				var mysqlFilename = Path.GetFullPath(filename).Replace("\\", "/");
				Session.CreateSQLQuery($@"
load data infile '{mysqlFilename}' replace into table OrderLines
(
	`OrderId`,
	`OfferId`,
	`ProductId`,
	CatalogId,
	`ProducerId`,
	`ProductSynonymId`,
	`ProducerSynonymId`,
	`Code`,
	`CodeCr`,
	`ProductSynonym`,
	`ProducerSynonym`,
	`Cost`,
	`Junk`,
	OriginalJunk,
	`Count`,
	`RequestRatio`,
	`MinOrderSum`,
	`MinOrderCount`,
	`Quantity`,
	`Unit`,
	`Volume`,
	`Note`,
	`Period`,
	`Doc`,
	`RegistryCost`,
	`VitallyImportant`,
	`ProducerCost`,
	`NDS`,
	`Comment`,
	`BarCode`,
	`CodeOKP`,
	`Series`,
	`Exp`
);

update OrderLines l
join Orders o on o.Id = l.OrderId
set l.RegionId = o.RegionId
where l.RegionId = 0;")
					.ExecuteUpdate();
				File.Delete(filename);
			}

			filename = FileHelper.MakeRooted("SentOrders.txt");
			if (File.Exists(filename)) {
				var mysqlFilename = Path.GetFullPath(filename).Replace("\\", "/");
				Session.CreateSQLQuery($@"
load data infile '{mysqlFilename}' replace into table SentOrders
(
	Id,
	ServerId,
	Addressid,
	PriceId,
	RegionId,
	CreatedOn,
	SentOn,
	PersonalComment,
	Comment,
	PriceDate
);")
					.ExecuteUpdate();
				File.Delete(filename);
			}

			filename = FileHelper.MakeRooted("SentOrderLines.txt");
			if (File.Exists(filename)) {
				var mysqlFilename = Path.GetFullPath(filename).Replace("\\", "/");
				Session.CreateSQLQuery($@"
load data infile '{mysqlFilename}' replace into table SentOrderLines
(
	Id,
	OrderId,
	ServerId,
	ServerOrderId,
	ProductId,
	CatalogId,
	ProducerId,
	`ProductSynonymId`,
	`ProducerSynonymId`,
	`Code`,
	`CodeCr`,
	`ProductSynonym`,
	`ProducerSynonym`,
	`Cost`,
	`Junk`,
		OriginalJunk,
	`Count`,
	`RequestRatio`,
	`MinOrderSum`,
	`MinOrderCount`,
	`Quantity`,
	`Unit`,
	`Volume`,
	`Note`,
	`Period`,
	`Doc`,
	`RegistryCost`,
	`VitallyImportant`,
	`ProducerCost`,
	`NDS`,
	`Comment`,
	`BarCode`,
	`CodeOKP`,
	`Series`,
	`Exp`
);

update SentOrders o
set LinesCount = (select count(*) from SentOrderLines l where o.Id = l.OrderId)
where LinesCount = 0;

update SentOrders o
set Sum = (select Sum(l.Count * l.Cost) from SentOrderLines l where o.Id = l.OrderId)
where Sum = 0;")
					.ExecuteUpdate();
				File.Delete(filename);
			}

			MigrateTable("Waybills.txt", @"
load data infile '{0}' replace into table Waybills
(
	Id,
	DocumentDate,
	SupplierId,
	AddressId,
	ProviderDocumentId,
	WriteTime,
	IsCreatedByUser,
	UserSupplierName,
	InvoiceId,
	InvoiceDate,
	SellerName,
	SellerAddress,
	SellerINN,
	SellerKPP,
	ShipperNameAndAddress,
	ConsigneeNameAndAddress,
	BuyerName,
	BuyerAddress,
	BuyerINN,
	BuyerKPP
);");
			MigrateTable("WaybillLines.txt", @"
load data infile '{0}' replace into table WaybillLines
(
	Id,
	WaybillId,
	Product,
	Certificates,
	Period,
	Producer,
	Country,
	ProducerCost,
	RegistryCost,
	SupplierPriceMarkup,
	SupplierCostWithoutNDS,
	SupplierCost,
	Quantity,
	VitallyImportant,
	NDS,
	SerialNumber,
	RetailMarkup,
	RetailCost,
	Edited,
	Amount,
	NdsAmount,
	Unit,
	ExciseTax,
	BillOfEntryNumber,
	EAN13,
	ProductId,
	ProducerId,
	RejectId);");
			MigrateTable("WaybillOrders.txt", @"
load data infile '{0}' replace into table WaybillOrders (DocumentLineId, OrderLineId);");

			//при миграции данные для импорта хранатся в паки in,
			//глобальный конфиг нельзя менять иначе все последующая работа
			//будет вестись не там где нужно, создаем нужную конфигурация для данной операции
			Config = Config.Clone();
			Config.TmpDir = FileHelper.MakeRooted("In");
			var settings = Session.Query<Settings>().First();
			settings.WaybillDir = FileHelper.MakeRooted("Waybills");
			settings.RejectDir = FileHelper.MakeRooted("Rejects");
			settings.DocDir = FileHelper.MakeRooted("Docs");
			ProcessBatch(
				Session.Query<Waybill>().Where(w => w.Sum == 0).Select(x => x.Id).ToArray(),
				(s, x) => {
					foreach (var id in x) {
						s.Load<Waybill>(id).CalculateForMigrated(settings);
					}
				});

			Session.Flush();
			Import();
		}

		private void MigrateTable(string filename, string sql)
		{
			filename = FileHelper.MakeRooted(filename);
			if (File.Exists(filename)) {
				var mysqlFilename = Path.GetFullPath(filename).Replace("\\", "/");
				Session.CreateSQLQuery(String.Format(sql, mysqlFilename))
					.ExecuteUpdate();
				File.Delete(filename);
			}
		}

		public void Import()
		{
			var data = GetDbData(Directory.GetFiles(Config.UpdateTmpDir).Select(Path.GetFileName), Config.UpdateTmpDir);
			var maxBatchLineId = (uint?)Session.CreateSQLQuery("select max(Id) from BatchLines").UniqueResult<long?>();

			//будь бдителен ImportCommand очистит сессию
			RunCommand(new ImportCommand(data));
			var settings = Session.Query<Settings>().First();

			SyncOrders();

			var batchAddresses = Session.Query<BatchLine>().Where(l => l.Id > maxBatchLineId).Select(l => l.Address)
				.Distinct()
				.ToArray();
			if (batchAddresses.Any()) {
				Session.CreateSQLQuery("delete from BatchLines where AddressId in (:addressIds) and id <= :maxId")
					.SetParameter("maxId", maxBatchLineId)
					.SetParameterList("addressIds", batchAddresses.Select(a => a.Id).ToArray())
					.SetFlushMode(FlushMode.Always)
					.ExecuteUpdate();
				Session.CreateSQLQuery("update OrderLines ol " +
					"join Orders o on o.Id = ol.OrderId " +
					"left join BatchLines b on b.ExportId = ol.ExportBatchLineId " +
					"set ol.ExportId = null " +
					"where o.AddressId in (:addressIds) and b.Id is null")
					.SetParameterList("addressIds", batchAddresses.Select(a => a.Id).ToArray())
					.SetFlushMode(FlushMode.Always)
					.ExecuteUpdate();
			}

			var imports = data.Select(d => Path.GetFileNameWithoutExtension(d.Item1));
			var offersImported = imports.Contains("offers", StringComparer.OrdinalIgnoreCase);
			var ordersImported = imports.Contains("orders", StringComparer.OrdinalIgnoreCase);

			var postUpdate = new PostUpdate();
			postUpdate.IsRejected = CalculateRejects(settings);
			postUpdate.IsAwaited = offersImported && CalculateAwaited();
				//в режиме получения документов мы не должны предлагать а должны просто открывать
			if (!syncData.Match("Waybills"))
				postUpdate.IsDocsReceived = Session.Query<LoadedDocument>().Count(d => d.IsDocDelivered) > 0;

			if (postUpdate.IsMadeSenseToShow) {
				Results.Add(new DialogResult(postUpdate));
				result = UpdateResult.SilentOk;
			}

			var dirs = Config.KnownDirs(settings);
			var resultDirs = Directory.GetDirectories(Config.UpdateTmpDir)
				.Select(d => dirs.FirstOrDefault(r => r.Name.Match(Path.GetFileName(d))))
				.Where(d => d != null)
				.ToArray();

			if (syncData.Match("Waybills")) {
				if (!StatelessSession.Query<LoadedDocument>().Any()) {
					SuccessMessage = "Новых файлов документов нет.";
				}
				else if (StatelessSession.Query<LoadedDocument>().Any(d => d.IsDocDelivered)) {
					//если получили разобранные накладные мы не должны открывать ни файлы ни папки накладных даже
					//если установлена опция
					Results.Add(new ShellResult(s => s.ShowWaybills()));
					var dir = dirs.First(d => d.Name.Match("waybills"));
					dir.OpenDir = false;
					dir.OpenFiles = false;
				}
			}

			var confirm =  new ConfirmRequest(requestId);
			if (offersImported || ordersImported)
				RestoreOrders(confirm);

			foreach (var dir in settings.DocumentDirs)
				Directory.CreateDirectory(dir);

			resultDirs.Each(Move);
			Results.AddRange(ResultDir.OpenResultFiles(resultDirs));
			ProcessAttachments(resultDirs);

			Directory.Delete(Config.UpdateTmpDir, true);
			if (Clean)
				File.Delete(Config.ArchiveFile);
			WaitAndLog(Client.PutAsync("Main", confirm, Formatter), "Подтверждение обновления");
		}

		private bool CalculateAwaited()
		{
			return Session.CreateSQLQuery(@"
select count(*)
from AwaitedItems a
join Offers o on o.CatalogId = a.CatalogId and (o.ProducerId = a.ProducerId or a.ProducerId is null)")
				.UniqueResult<long?>() > 0;
		}

		private void ProcessAttachments(ResultDir[] resultDirs)
		{
			var dir = resultDirs.FirstOrDefault(d => d.Name.Match("attachments"));
			if (dir == null)
				return;
			foreach (var filename in dir.ResultFiles) {
				var id = SafeConvert.ToUInt32(Path.GetFileNameWithoutExtension(filename));
				var attachment =  Session.Get<Attachment>(id);
				if (attachment == null)
					continue;
				Session.Save(attachment.UpdateLocalFile(Path.GetFullPath(filename)));
				Session.Save(attachment);
			}
		}

		public bool CalculateRejects(Settings settings)
		{
			//позиции перестают считаться новыми только когда мы получаем новую порцию позиций которые являются забраковкой
			//.SetFlushMode(FlushMode.Auto)
			//по умолчанию запросы имеют flush mode Unspecified
			//это значит что изменения из сессии не будут сохранены в базу перед запросом
			//а попадут в базу после commit
			//те изменения из сессии перетрут состояние флагов

			//сопоставляем с учетом продукта и производителя
			var begin = DateTime.Today.AddDays(-settings.TrackRejectChangedDays);
			var exists =  Session.CreateSQLQuery("update (WaybillLines as l, Rejects r) " +
				"	join Waybills w on w.Id = l.WaybillId " +
				"set l.RejectId = r.Id, l.IsRejectNew = 2, w.IsRejectChanged = 2 " +
				"where l.RejectId is null " +
				"	and r.Canceled = 0" +
				"	and l.ProductId = r.ProductId " +
				"	and l.ProducerId = r.ProducerId " +
				"	and l.SerialNumber = r.Series " +
				"	and l.ProductId is not null " +
				"	and l.ProducerId is not null " +
				"	and l.SerialNumber is not null " +
				"	and w.WriteTime > :begin")
				.SetParameter("begin", begin)
				.SetFlushMode(FlushMode.Always)
				.ExecuteUpdate() > 0;
			//сопоставляем по продукту
			//это странно но тем не менее если у строки накладной ИЛИ у отказа производитель неизвестен то сопоставляем
			//по продукту и серии, так в analitf
			exists |= Session.CreateSQLQuery("update (WaybillLines as l, Rejects r) " +
				"	join Waybills w on w.Id = l.WaybillId " +
				"set l.RejectId = r.Id, l.IsRejectNew = 2, w.IsRejectChanged = 2 " +
				"where l.RejectId is null " +
				"	and r.Canceled = 0" +
				"	and l.ProductId = r.ProductId " +
				"	and (l.ProducerId is null or r.ProducerId is null) " +
				"	and l.SerialNumber = r.Series " +
				"	and l.ProductId is not null " +
				"	and l.SerialNumber is not null " +
				"	and w.WriteTime > :begin ")
				.SetParameter("begin", begin)
				.ExecuteUpdate() > 0;
			//сопоставляем по наименованию
			exists |= Session.CreateSQLQuery("update (WaybillLines as l, Rejects r) " +
				"	join Waybills w on w.Id = l.WaybillId " +
				"set l.RejectId = r.Id, l.IsRejectNew = 2, w.IsRejectChanged = 2 " +
				"where l.RejectId is null " +
				"	and r.Canceled = 0" +
				"	and l.ProductId is null " +
				"	and l.Product is not null " +
				"	and l.SerialNumber is not null " +
				"	and l.Product = r.Product " +
				"	and l.SerialNumber = r.Series " +
				"	and w.WriteTime > :begin")
				.SetParameter("begin", begin)
				.ExecuteUpdate() > 0;

			exists |= Session.CreateSQLQuery("update WaybillLines as l " +
				"	join Rejects r on r.Id = l.RejectId " +
				"	join Waybills w on w.Id = l.WaybillId " +
				"set l.IsRejectCanceled = 1, l.RejectId = null, w.IsRejectChanged = 2 " +
				"where l.RejectId is not null " +
				"	and r.Canceled = 1" +
				"	and w.WriteTime > :begin")
				.SetParameter("begin", begin)
				.ExecuteUpdate() > 0;

			if (exists) {
				Session.CreateSQLQuery("update WaybillLines set IsRejectNew = 0 where IsRejectNew = 1")
					.ExecuteUpdate();
				Session.CreateSQLQuery("update WaybillLines set IsRejectNew = 1 where IsRejectNew = 2")
					.ExecuteUpdate();
				Session.CreateSQLQuery("update Waybills set IsRejectChanged = 0 where IsRejectChanged = 1")
					.ExecuteUpdate();
				Session.CreateSQLQuery("update Waybills set IsRejectChanged = 1 where IsRejectChanged = 2")
					.ExecuteUpdate();
			}
			Session.CreateSQLQuery("delete from Rejects where Canceled = 1")
				.ExecuteUpdate();

			return exists;
		}

		private void SyncOrders()
		{
			Session.CreateSQLQuery("update OrderLines ol "
				+ "join Orders o on o.ExportId = ol.ExportOrderId "
				+ "set ol.OrderId = o.Id "
				+ "where ol.ExportOrderId is not null and ol.OrderId is null;"
				+ "update OrderLines set ExportOrderId = null;")
				.ExecuteUpdate();

			var loaded = Session.Query<Order>().Where(o => o.LinesCount == 0).ToArray();
			foreach (var order in loaded) {
				order.UpdateStat();
			}
		}

		private void RestoreOrders(ConfirmRequest confirm)
		{
			var orders = Session.Query<Order>()
				.Fetch(o => o.Address)
				.Fetch(o => o.Price)
				.ToArray();

			var ordersToRestore = orders.Where(o => !o.Frozen).ToArray();

			//если это автозаказ то мы не должны восстанавливать заказы
			//по адресам доставки которые участвовали в автозаказе
			//суть в том что после автозаказа по тем адресам по которым автозаказ производился заказы должны быть заморожены
			//для этого мы исключаем из восстановления заказа у которые не являются результатом автозаказа (ExportId != null) и
			//и относятся к адресам где автозаказ производился
			if (SyncData.Match("Batch")) {
				var autoOrderAddress = ordersToRestore
					.Where(o => o.Lines.Any(l => l.ExportId != null))
					.Select(o => o.Address).Distinct().ToArray();
				ordersToRestore = ordersToRestore
					.Where(o => !autoOrderAddress.Contains(o.Address) || o.Lines.Any(l => l.ExportId != null))
					.ToArray();

				//если это повторная обработка, удаляем старые заказы
				if (BatchFile == null) {
					var todelete = orders.Where(o => autoOrderAddress.Contains(o.Address)).Except(ordersToRestore).ToArray();
					orders = orders.Except(todelete).ToArray();
					Session.DeleteEach(todelete);
				}

				var loaded = orders.Where(x => x.SkipRestore).ToArray();
				loaded.Each(x => x.SkipRestore = false);
				orders = orders.Except(loaded).ToArray();
			}
			else {
				//todo наверное это можно использовать и при загрузке автозаказа, протестировать
				//если мы загрузили заказ с сервера а у нас уже есть заказ на этого поставщика мы должны заморозить существующий
				var loaded = ordersToRestore.Where(x => x.IsLoaded).ToArray();
				if (loaded.Length > 0)
					confirm.Message = "Экспортированы неподтвержденные заявки: " + loaded.Implode(x => $"{x.ExportId} -> {x.Id}");
				var loadedIds = loaded.Select(x => x.SafePrice.Id).ToArray();
				ordersToRestore = ordersToRestore.Where(x => x.IsLoaded || !loadedIds.Contains(x.SafePrice.Id)).ToArray();
			}


			//нужно сбросить статус для ранее замороженных заказов что бы они не отображались в отчете
			//каждый раз
			orders.Each(o => {
				o.ResetStatus();
				o.Frozen = true;
			});
			var command = new UnfreezeCommand<Order>(ordersToRestore.Select(o => o.Id).ToArray());
			command.Restore = true;
			var report = (string)RunCommand(command);

			var user = Session.Query<User>().First();
			if (user.IsPreprocessOrders) {
				var problemCount = Session.Query<OrderLine>()
					.Count(l => l.SendResult != LineResultStatus.OK
						|| l.Order.SendResult != OrderResultStatus.OK);
				if (problemCount > 0)
					Results.Add(new DialogResult(new Correction(), fullScreen: true));
			}
			else {
				if (!String.IsNullOrEmpty(report)) {
					//формы должны показываться в определенном порядке
					Results.Add(new DialogResult(new DocModel<TextDoc>(new TextDoc("Ненайденные позиции", report)),
						fixedSize: true));
					Results.Add(new MessageResult(SuccessMessage));
					result = UpdateResult.SilentOk;
				}
			}
		}

		public void Move(ResultDir source)
		{
			if (source.Clean) {
				if (Directory.Exists(source.Dst)) {
					Directory.Delete(source.Dst, true);
					Directory.CreateDirectory(source.Dst);
				}
			}

			if (source.GroupBySupplier) {
				source.ResultFiles = MoveToPerSupplierDir(source.Src, DocumentType.Waybills);
			}
			else {
				source.ResultFiles = Move(source.Src, source.Dst);
			}
		}

		private static List<string> Move(string source, string destination)
		{
			var files = new List<string>();
			Directory.CreateDirectory(destination);
			foreach (var file in Directory.GetFiles(source)) {
				var dst = Path.Combine(destination, Path.GetFileName(file));
				if (File.Exists(dst))
					File.Delete(dst);
				File.Move(file, dst);
				files.Add(dst);
			}
			return files;
		}

		private List<string> MoveToPerSupplierDir(string srcDir, DocumentType type)
		{
			var result = new List<string>();
			if (!Directory.Exists(srcDir))
				return result;

			var waybills = StatelessSession.Query<LoadedDocument>()
				.Fetch(d => d.Supplier)
				.Where(d => d.Type == type && d.Supplier.Name != null);
			var maps = StatelessSession.Query<DirMap>()
				.Fetch(m => m.Supplier)
				.Where(m => m.Supplier.Name != null)
				.ToList();
			foreach (var doc in waybills) {
				try {
					if (String.IsNullOrEmpty(doc.OriginFilename))
						continue;

					var map = maps.First(m => m.Supplier.Id == doc.Supplier.Id);
					var dst = map.Dir;
					if (!Directory.Exists(dst))
						FileHelper.CreateDirectoryRecursive(dst);

					var files = Directory.GetFiles(srcDir, String.Format("{0}_*", doc.Id));
					foreach (var src in files) {
						dst = FileHelper.Uniq(Path.Combine(dst, doc.OriginFilename));
						File.Move(src, dst);
						result.Add(dst);
					}
				}
				catch(Exception e) {
					Log.Error("Ошибка перемещения файла", e);
				}
			}
			return result;
		}

		private static List<Tuple<string, string[]>> GetDbData(IEnumerable<string> files, string tmpDir)
		{
			return files.Where(f => f.EndsWith("meta.txt"))
				.Select(f => Tuple.Create(f, files.FirstOrDefault(d => Path.GetFileNameWithoutExtension(d)
					.Match(f.Replace(".meta.txt", "")))))
				.Where(t => t.Item2 != null)
				.Select(t => Tuple.Create(
					Path.GetFullPath(Path.Combine(tmpDir, t.Item2)),
					File.ReadAllLines(Path.Combine(tmpDir, t.Item1))))
				.Concat(files.Where(x => Path.GetFileNameWithoutExtension(x).Match("cmds"))
					.Select(x => Tuple.Create(Path.Combine(tmpDir, x), new string[0])))
				.ToList();
		}

		private static void Download(HttpResponseMessage response, string filename, ProgressReporter reporter)
		{
			using (var file = File.Create(filename))
			using (var stream = response.Content.ReadAsStreamAsync().Result) {
				reporter.Weight((int)response.Content.Headers.ContentLength.GetValueOrDefault());
				var buffer = new byte[4*1024];
				int count;
				while ((count = stream.Read(buffer, 0, buffer.Length)) != 0) {
					file.Write(buffer, 0, count);
					reporter.Progress(count);
				}
			}
		}

		private void SendPrices(HttpClient client, Settings settings, CancellationToken token)
		{
			var lastUpdate = settings.LastUpdate;
			var prices = Session.Query<Price>().Where(p => p.Timestamp > lastUpdate).ToArray();
			var clientPrices = prices.Select(p => new PriceSettings(p.Id.PriceId, p.Id.RegionId, p.Active)).ToArray();
			if (clientPrices.Length == 0)
				return;

			CheckResult(client.PostAsync("Main", new SyncRequest(clientPrices), Formatter, token));
		}

		private void WaitAndLog(Task<HttpResponseMessage> task, string name)
		{
			if (task == null)
				return;

			try {
				task.Wait();
				if (!IsOkStatusCode(task.Result?.StatusCode))
					Log.Error($"Задача '{name}' завершилась ошибкой {task.Result}");
			}
			catch(AggregateException e) {
				Log.Error($"Задача '{name}' завершилась ошибкой", e.GetBaseException());
			}
		}

		public async Task<HttpResponseMessage> SendLogs(HttpClient client, CancellationToken token)
		{
			using (var cleaner = new FileCleaner()) {
				var file = cleaner.TmpFile();

				//TODO: в тестах сброс конфигурации может сильно испортить жизнь
				//например если нужно отладить запросы
				LogManager.ResetConfiguration();
				try
				{
					var logs = Directory.GetFiles(FileHelper.MakeRooted("."), "*.log")
						.Where(f => new FileInfo(f).Length > 0)
						.ToArray();

					if (logs.Length == 0)
						return null;

					using(var zip = new ZipFile()) {
						foreach (var logFile in logs) {
							zip.AddFile(logFile);
						}
						zip.Save(file);
					}

					var logsWatch = new FileCleaner();
					logsWatch.Watch(logs);
					logsWatch.Dispose();
				}
				finally {
					XmlConfigurator.Configure();
				}

				using (var stream = File.OpenRead(file))
					return await client.PostAsync("Logs", new StreamContent(stream), token);
			}
		}

		public void Diadok()
		{
			var settings = Session.Query<Settings>().First();
			if (String.IsNullOrEmpty(settings.DiadokUsername) || String.IsNullOrEmpty(settings.DiadokPassword))
				return;
			var api = new DiadocApi(Config.DiadokApiKey, Config.DiadokUrl, new WinApiCrypt());
			var token = api.Authenticate(settings.DiadokUsername, settings.DiadokPassword);

			var toSign = Session.Query<Waybill>().Where(x => x.DocType == DocType.Diadok && x.IsSign && !x.IsSigned)
				.ToArray();
			foreach (var waybill in toSign) {
				var patch = new MessagePatchToPost {
					BoxId = waybill.DiadokBoxId,
					MessageId = waybill.DiadokMessageId
				};
				var sign = new DocumentSignature {
					ParentEntityId = waybill.DiadokEnityId,
				};
				if (settings.DebugUseTestSign) {
					sign.SignWithTestSignature = true;
				}
				else {
					sign.Signature = Session.Load<Sign>(waybill.Id).SignBytes;
				}
				patch.AddSignature(sign);
				Log.InfoFormat("Попытка подписать вложение {0} документа {1}", waybill.DiadokEnityId, waybill.DiadokMessageId);
				try {
					api.PostMessagePatch(token, patch);
					waybill.IsSigned = true;
					var fullname = waybill.TryGetFile(settings);
					Session.Save(new JournalRecord {
						CreateAt = DateTime.Now,
						Name = String.Format("Подписан документ Диадок {0}", waybill.Filename),
						Filename = fullname
					});
					Log.InfoFormat("Подписано вложение {0} документа {1}", waybill.DiadokEnityId, waybill.DiadokMessageId);
				}
				catch(WebException e) {
					var http = e.Response as HttpWebResponse;
					if (http != null && (int)http.StatusCode == 409) {
						Log.Warn(String.Format("Не удалось подписать документ {0}. Документ уже подписан.", waybill.Filename), e);
						Results.Add(MessageResult.Error("Не удалось подписать документ {0}. Документ уже подписан.", waybill.Filename));
					}
					else {
						throw;
					}
				}
			}

			var boxes = api.GetMyOrganizations(token).Organizations.SelectMany(x => x.Boxes);
			foreach (var box in boxes) {
				var documentsFilter = new DocumentsFilter {
					FilterCategory = "Any.Inbound",
					BoxId = box.BoxId,
					SortDirection = "Descending"
				};

				DocumentList documents;
				do {
					documents = api.GetDocuments(token, documentsFilter);
					var endFound = false;
					foreach (var doc in documents.Documents.Where(x => !x.IsDeleted)) {
						documentsFilter.AfterIndexKey = doc.IndexKey;
						if (Session.Query<Waybill>().Any(x => x.DiadokMessageId == doc.MessageId)) {
							endFound = true;
							break;
						}

						var supplier = Session.Query<Supplier>().FirstOrDefault(x => x.DiadokOrgId == doc.CounteragentBoxId);
						var message = api.GetMessage(token, box.BoxId, doc.MessageId);
						var files = message.Entities
							.Where(e => e.EntityType == EntityType.Attachment
								&& !String.IsNullOrEmpty(e.FileName)
								&& e.AttachmentType == AttachmentType.Nonformalized);
						foreach (var file in files) {
							var waybill = new Waybill {
								DiadokMessageId = doc.MessageId,
								DiadokBoxId = box.BoxId,
								DiadokEnityId = file.EntityId,
								DocType = DocType.Diadok,
								Filename = file.FileName,
								//подписывать надо только если документ не был подписан ранее и требует подписи
								IsSigned = doc.NonformalizedDocumentMetadata.DocumentStatus
									!= NonformalizedDocumentStatus.InboundWaitingForRecipientSignature,
								IsNew = true,
								WriteTime = DateTime.Now,
								ProviderDocumentId = String.IsNullOrEmpty(doc.CustomDocumentId) ? doc.MessageId : doc.CustomDocumentId,
								DocumentDate = NullableConvert.ToDateTime(doc.DocumentDate).GetValueOrDefault(doc.CreationTimestamp),
								Supplier = supplier,
								UserSupplierName = supplier == null ? message.FromTitle : null,
							};
							Session.Save(waybill);
							var path = settings.MapPath("Waybills");
							Directory.CreateDirectory(path);
							var fullname = Path.Combine(path, waybill.Id + "_" + FileHelper.StringToFileName(file.FileName));
							if (fullname.Length > 260)
								fullname = Path.Combine(path, waybill.Id + Path.GetExtension(file.FileName));
							File.WriteAllBytes(fullname, file.Content.Data);
							Log.InfoFormat("Получен документ Диадок {0} файл {1}", doc.MessageId, file.FileName);
							Session.Save(new JournalRecord {
								CreateAt = DateTime.Now,
								Name = String.Format("Загружен документ Диадок {0}", file.FileName),
								Filename = fullname
							});
						}
					}
					if (endFound)
						break;
				} while (documents.TotalCount > documents.Documents.Count);
			}
		}
	}
}
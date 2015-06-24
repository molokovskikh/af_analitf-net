using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Orders;
using Common.NHibernate;
using Common.Tools;
using Ionic.Zip;
using log4net;
using log4net.Config;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Linq;

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
			uint requestId = 0;
			if (SyncData.Match("Batch")) {
				updateType = "автозаказ";
				SuccessMessage = "Автоматическая обработка дефектуры завершена.";
				response = Wait("Batch", Client.PostAsync("Batch", GetBatchRequest(user), Token), ref requestId);
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
				var lastSync = user == null ? null : user.LastSync;
				if (lastSync == null) {
					updateType = "куммулятивное";
				}
				if (syncData.Match("Waybills")) {
					updateType = "загрузка накладных";
				}
				var url = Config.SyncUrl(syncData, lastSync);
				SendPrices(Client, Token);
				var request = Client.GetAsync(url, Token);
				response = Wait(Config.WaitUrl(url, syncData).ToString(), request, ref requestId);
			}

			Reporter.Stage("Загрузка данных");
			Log.InfoFormat("Запрос обновления, тип обновления '{0}'", updateType);
			Download(response, Config.ArchiveFile, Reporter);
			Log.InfoFormat("Обновление загружено, размер {0}", new FileInfo(Config.ArchiveFile).Length);
			var result = ProcessUpdate(Config.ArchiveFile);
			Log.InfoFormat("Обновление завершено успешно");

			WaitAndLog(sendLogsTask, "Отправка логов");
			return result;
		}

		private HttpContent GetBatchRequest(User user)
		{
			var request = new BatchRequest(AddressId, user == null ? null : user.LastSync);
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

		public void Import()
		{
			List<Tuple<string, string[]>> data;
			using (var zip = new ZipFile(Config.ArchiveFile))
				data = GetDbData(zip.Select(z => z.FileName), Config.UpdateTmpDir);

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

			if (offersImported || ordersImported)
				RestoreOrders();

			foreach (var dir in settings.DocumentDirs)
				Directory.CreateDirectory(dir);

			resultDirs.Each(Move);
			Results.AddRange(ResultDir.OpenResultFiles(resultDirs));
			ProcessAttachments(resultDirs);

			Directory.Delete(Config.UpdateTmpDir, true);
			if (Clean)
				File.Delete(Config.ArchiveFile);
			WaitAndLog(Client.DeleteAsync("Main"), "Подтверждение обновления");
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
				+ "where ol.ExportOrderId is not null;"
				+ "update OrderLines set ExportOrderId = null;"
				+ "update Orders set ExportId = null")
				.ExecuteUpdate();

			var loaded = Session.Query<Order>().Where(o => o.LinesCount == 0).ToArray();
			foreach (var order in loaded) {
				order.UpdateStat();
			}
		}

		private void RestoreOrders()
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
			if (Directory.Exists(source)) {
				Directory.CreateDirectory(destination);

				foreach (var file in Directory.GetFiles(source)) {
					var dst = Path.Combine(destination, Path.GetFileName(file));
					if (File.Exists(dst))
						File.Delete(dst);
					File.Move(file, dst);
					files.Add(dst);
				}
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

		private void SendPrices(HttpClient client, CancellationToken token)
		{
			var settings = Session.Query<Settings>().First();
			var lastUpdate = settings.LastUpdate;
			var prices = Session.Query<Price>().Where(p => p.Timestamp > lastUpdate).ToArray();
			var clientPrices = prices.Select(p => new PriceSettings(p.Id.PriceId, p.Id.RegionId, p.Active)).ToArray();
			if (clientPrices.Length == 0)
				return;

			var response = client.PostAsync("Main", new SyncRequest(clientPrices), Formatter, token).Result;
			CheckResult(response);
		}

		private void WaitAndLog(Task<HttpResponseMessage> task, string name)
		{
			if (task == null)
				return;

			try {
				task.Wait();
				if (!IsOkStatusCode(task.Result.StatusCode))
					Log.ErrorFormat("Задача '{0}' завершилась ошибкой {1}", name, task.Result.StatusCode);
			}
			catch(AggregateException e) {
				Log.Error(String.Format("Задача '{0}' завершилась ошибкой", name), e.GetBaseException());
			}
		}

		public Task<HttpResponseMessage> SendLogs(HttpClient client, CancellationToken token)
		{
			var file = Path.GetTempFileName();
			var cleaner = new FileCleaner();
			cleaner.Watch(file);

			//TODO: в тестах сброс конфигурации может сильно испортить жизнь
			//например если нужно отладить запросы
			LogManager.ResetConfiguration();
			try
			{
				var logs = Directory.GetFiles(Config.RootDir, "*.log")
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

			var stream = File.OpenRead(file);
			var post = client.PostAsync("Logs", new StreamContent(stream), token);
			//TODO мы никогда не узнаем об ошибке
			post.ContinueWith(t => {
				stream.Dispose();
				cleaner.Dispose();
			});
			return post;
		}
	}
}
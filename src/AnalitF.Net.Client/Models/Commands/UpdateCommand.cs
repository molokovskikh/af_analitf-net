using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Orders;
using Common.Tools;
using Ionic.Zip;
using log4net.Appender;
using log4net.Repository.Hierarchy;
using NHibernate;
using NHibernate.Linq;
using log4net.Config;
using LogManager = log4net.LogManager;

namespace AnalitF.Net.Client.Models.Commands
{
	public class ResultDir
	{
		private static string[] autoopen = {
			"rejects", "waybills", "attachments"
		};

		public ResultDir(string name, Settings settings, Config.Config confg)
		{
			Name = name;
			Src = Path.Combine(confg.UpdateTmpDir, Name);
			Dst = settings.MapPath(name) ?? Path.Combine(confg.RootDir, name);
			if (name.Match("waybills")) {
				GroupBySupplier = settings.GroupWaybillsBySupplier;
			}
			if (autoopen.Any(d => d.Match(name))) {
				Open = settings.OpenRejects;
			}
		}

		public string Name { get; set; }
		public string Src { get; set; }
		public string Dst { get; set; }
		public bool Open { get; set; }
		public bool GroupBySupplier;
		public IList<string> ResultFiles = new List<string>();
	}

	public class UpdateCommand : RemoteCommand
	{
		public bool Clean = true;
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

			var queryString = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>("reset", "true"),
				new KeyValuePair<string, string>("data", syncData)
			};

			var stringBuilder = new StringBuilder();
			foreach (var keyValuePair in queryString) {
				if (stringBuilder.Length > 0)
					stringBuilder.Append('&');
				stringBuilder.Append(Uri.EscapeDataString(keyValuePair.Key));
				stringBuilder.Append('=');
				stringBuilder.Append(Uri.EscapeDataString(keyValuePair.Value));
			}
			var builder = new UriBuilder(new Uri(Config.BaseUrl, "Main")) {
				Query = stringBuilder.ToString(),
			};
			var url = builder.Uri;

			var sendLogsTask = SendLogs(Client, Token);
			SendPrices(Client, Token);

			var response = Wait(new Uri(Config.BaseUrl, "Main"), Client.GetAsync(url, Token));
			Reporter.Stage("Загрузка данных");
			Download(response, Config.ArchiveFile, Reporter);
			var result = ProcessUpdate();

			WaitAndLog(sendLogsTask, "Отправка логов");
			return result;
		}

		private HttpResponseMessage Wait(Uri url, Task<HttpResponseMessage> task)
		{
			var done = false;
			HttpResponseMessage response = null;
			while (!done) {
				response = task.Result;
				if (response.StatusCode != HttpStatusCode.OK
					&& response.StatusCode != HttpStatusCode.Accepted)
					throw new RequestException(
						String.Format("Произошла ошибка при обработке запроса, код ошибки {0} {1}",
							response.StatusCode,
							response.Content.ReadAsStringAsync().Result),
						response.StatusCode);

				done = response.StatusCode == HttpStatusCode.OK;
				Reporter.Stage("Подготовка данных");
				if (!done) {
					Token.WaitHandle.WaitOne(Config.RequestInterval);
					Token.ThrowIfCancellationRequested();
				}

				task = Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, Token);
			}
			return response;
		}

		public UpdateResult ProcessUpdate()
		{
			if (Directory.Exists(Config.UpdateTmpDir))
				Directory.Delete(Config.UpdateTmpDir, true);

			if (!Directory.Exists(Config.UpdateTmpDir))
				Directory.CreateDirectory(Config.UpdateTmpDir);

			using (var zip = new ZipFile(Config.ArchiveFile)) {
				zip.ExtractAll(Config.UpdateTmpDir, ExtractExistingFileAction.OverwriteSilently);
			}

			if (File.Exists(Path.Combine(Config.UpdateTmpDir, "update", "Updater.exe")))
				return UpdateResult.UpdatePending;

			Import();
			return result;
		}

		public void Import()
		{
			if (Reporter == null)
				Reporter = new ProgressReporter(Progress);

			var settings = Session.Query<Settings>().First();

			List<System.Tuple<string, string[]>> data;
			using (var zip = new ZipFile(Config.ArchiveFile))
				data = GetDbData(zip.Select(z => z.FileName));

			RunCommand(new ImportCommand(data));

			SyncOrders();
			CalculateRejects(settings);

			if (syncData.Match("Waybills") && !StatelessSession.Query<LoadedDocument>().Any()) {
				SuccessMessage = "Новых файлов документов нет.";
			}

			var offersImported = data.Any(t => Path.GetFileNameWithoutExtension(t.Item1).Match("offers"));
			if (offersImported) {
				RestoreOrders();
			}

			foreach (var dir in settings.DocumentDirs)
				FileHelper.CreateDirectoryRecursive(dir);

			var dirs = new List<ResultDir> {
				new ResultDir("ads", settings, Config),
				new ResultDir("newses", settings, Config),
				new ResultDir("waybills", settings, Config),
				new ResultDir("docs", settings, Config),
				new ResultDir("rejects", settings, Config),
				new ResultDir("attachments", settings, Config),
			};

			var resultDirs = Directory.GetDirectories(Config.UpdateTmpDir)
				.Select(d => dirs.FirstOrDefault(r => r.Name.Match(Path.GetFileName(d))))
				.Where(d => d != null)
				.ToArray();

			resultDirs.Each(Move);
			OpenResultFiles(resultDirs);
			ProcessAttachments(resultDirs);

			Directory.Delete(Config.UpdateTmpDir, true);
			if (Clean)
				File.Delete(Config.ArchiveFile);
			WaitAndLog(Confirm(), "Подтверждение обновления");
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
			//это значит что изменения из сесси не будут сохранены в базу перед запросом
			//а попадут в базу после commit
			//те изменеия из сесии перетрут состояние флагов
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

			exists |= Session.CreateSQLQuery("update (WaybillLines as l, Rejects r) " +
				"	join Waybills w on w.Id = l.WaybillId " +
				"set l.RejectId = r.Id, l.IsRejectNew = 2, w.IsRejectChanged = 2 " +
				"where l.RejectId is null " +
				"	and r.Canceled = 0" +
				"	and l.ProductId = r.ProductId " +
				"	and l.ProducerId is null " +
				"	and r.ProducerId is null " +
				"	and l.SerialNumber = r.Series " +
				"	and l.ProductId is not null " +
				"	and l.SerialNumber is not null " +
				"	and w.WriteTime > :begin ")
				.SetParameter("begin", begin)
				.ExecuteUpdate() > 0;

			exists |= Session.CreateSQLQuery("update (WaybillLines as l, Rejects r) " +
				"	join Waybills w on w.Id = l.WaybillId " +
				"set l.RejectId = r.Id, l.IsRejectNew = 2, w.IsRejectChanged = 2 " +
				"where l.RejectId is null " +
				"	and r.Canceled = 0" +
				"	and l.ProductId = r.ProductId"  +
				"	and l.ProducerId is null " +
				"	and r.ProducerId is null " +
				"	and l.SerialNumber = r.Series " +
				"	and l.ProductId is not null " +
				"	and l.SerialNumber is not null " +
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

				Results.Add(new DialogResult(new PostUpdate(), @fixed: true));
				result = UpdateResult.Other;
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
				order.LinesCount = order.Lines.Count;
				order.Sum = order.Lines.Sum(l => l.Sum);
			}
		}

		private void RestoreOrders()
		{
			var orders = Session.Query<Order>()
				.Fetch(o => o.Address)
				.Fetch(o => o.Price)
				.ToArray();

			var ids = orders.Where(o => !o.Frozen).Select(o => o.Id).ToArray();
			//нужно сбросить статус для ранее замороженных заказов что бы они не отображались в отчете
			//каждый раз
			orders.Each(o => o.ResetStatus());
			orders.Each(o => o.Frozen = true);
			var command = new UnfreezeCommand<Order>(ids);
			command.CalculateStatus = true;
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
					Results.Add(new DialogResult(new TextViewModel(report) {
						Header = "Предложения по данным позициям из заказа отсутствуют",
						DisplayName = "Не найденные позиции"
					}, @fixed: true));
				}
			}
		}

		private void Move(ResultDir source)
		{
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
				if (!Directory.Exists(destination)) {
					Directory.CreateDirectory(destination);
				}

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
				.Where(d => d.Type == type && d.Supplier != null);
			//todo review query
			var maps = StatelessSession.Query<DirMap>()
				.Fetch(m => m.Supplier)
				.Where(m => m.Supplier != null)
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
						dst = FileHelper2.Uniq(Path.Combine(dst, doc.OriginFilename));
						File.Move(src, dst);
						result.Add(dst);
					}
				}
				catch(Exception e) {
					log.Error("Ошибка перемещения файла", e);
				}
			}
			return result;
		}

		private void OpenResultFiles(IEnumerable<ResultDir> groups)
		{
			var toOpen = groups.Where(g => g.Open);
			var openDir = toOpen.Sum(g => g.ResultFiles.Count) > 5;

			foreach (var dir in toOpen) {
				if (dir.ResultFiles.Count > 0) {
					if (openDir) {
						Results.Add(new OpenResult(dir.Dst));
					}
					else {
						Results.AddRange(dir.ResultFiles.Select(f => new OpenResult(f)));
					}
				}
			}
		}

		private List<System.Tuple<string, string[]>> GetDbData(IEnumerable<string> files)
		{
			return files.Where(f => f.EndsWith("meta.txt"))
				.Select(f => Tuple.Create(f, files.FirstOrDefault(d => Path.GetFileNameWithoutExtension(d)
					.Match(f.Replace(".meta.txt", "")))))
				.Where(t => t.Item2 != null)
				.Select(t => Tuple.Create(
					Path.GetFullPath(Path.Combine(Config.UpdateTmpDir, t.Item2)),
					File.ReadAllLines(Path.Combine(Config.UpdateTmpDir, t.Item1))))
				.ToList();
		}

		private static void Download(HttpResponseMessage response, string filename, ProgressReporter reporter)
		{
			using (var file = File.Create(filename)) {
				var stream = response.Content.ReadAsStreamAsync().Result;
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

			var response = client.PostAsync(new Uri(Config.BaseUrl, "Main").ToString(),
				new SyncRequest(clientPrices),
				Formatter,
				token)
				.Result;
			CheckResult(response);
		}

		private void WaitAndLog(Task<HttpResponseMessage> task, string name)
		{
			if (task == null)
				return;

			try {
				task.Wait();
				if (!IsOkStatusCode(task.Result.StatusCode))
					log.ErrorFormat("Задача '{0}' завершилась ошибкой {1}", name, task.Result.StatusCode);
			}
			catch(AggregateException e) {
				log.Error(String.Format("Задача '{0}' завершилась ошибкой", name), e.GetBaseException());
			}
		}

		public Task<HttpResponseMessage> Confirm()
		{
			return Client.DeleteAsync(new Uri(Config.BaseUrl, "Main"));
		}

		public Task<HttpResponseMessage> SendLogs(HttpClient client, CancellationToken token)
		{
			var file = Path.GetTempFileName();
			var cleaner = new FileCleaner();
			cleaner.Watch(file);

			//TODO: в тестах сброс конфигурации может сильно испортить жизнь
			//например если нужно подебажить запросы
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

			var uri = new Uri(Config.BaseUrl, "Logs");
			var stream = File.OpenRead(file);
			var post = client.PostAsync(uri, new StreamContent(stream), token);
			//TODO мы никогда не узнаем об ошибке
			post.ContinueWith(t => {
				stream.Dispose();
				cleaner.Dispose();
			});
			return post;
		}
	}
}
﻿using System;
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
using Common.Tools;
using Ionic.Zip;
using NHibernate.Linq;
using log4net.Config;
using LogManager = log4net.LogManager;

namespace AnalitF.Net.Client.Models.Commands
{
	public class ResultDir
	{
		public ResultDir(string name, Settings settings, string dstRoot, string srcRoot)
		{
			Name = name;
			Src = Path.Combine(srcRoot, Name);
			Dst = settings.MapPath(name) ?? Path.Combine(dstRoot, name);
			if (name.Match("waybills")) {
				Open = settings.OpenWaybills;
				GroupBySupplier = settings.GroupWaybillsBySupplier;
			}
			if (name.Match("rejects")) {
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
		private string archiveFile;
		private string extractPath;
		private string logPath;
		private string rootPath;

		public ProgressReporter Reporter;
		public string[] SyncData = new string[0];

		public UpdateCommand(string file, string extractPath, string rootPath)
		{
			ErrorMessage = "Не удалось получить обновление. Попробуйте повторить операцию позднее.";
			SuccessMessage = "Обновление завершено успешно.";
			this.archiveFile = file;
			this.extractPath = extractPath;
			this.logPath = rootPath;
			this.rootPath = rootPath;
		}

		protected override UpdateResult Execute()
		{
			Reporter = new ProgressReporter(Progress);
			Reporter.StageCount(4);

			var queryString = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>("reset", "true")
			};
			foreach (var data in SyncData) {
				queryString.Add(new KeyValuePair<string, string>("data", data));
			}

			var stringBuilder = new StringBuilder();
			foreach (var keyValuePair in queryString) {
				if (stringBuilder.Length > 0)
					stringBuilder.Append('&');
				stringBuilder.Append(Uri.EscapeDataString(keyValuePair.Key));
				stringBuilder.Append('=');
				stringBuilder.Append(Uri.EscapeDataString(keyValuePair.Value));
			}
			var builder = new UriBuilder(new Uri(BaseUri, "Main")) {
				Query = stringBuilder.ToString(),
			};
			var url = builder.Uri;

			var sendLogsTask = SendLogs(Client, Token);
			SendPrices(Client, Token);

			var response = Wait(new Uri(BaseUri, "Main"), Client.GetAsync(url, Token));
			Reporter.Stage("Загрузка данных");
			Download(response, archiveFile, Reporter);
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
					Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(15));
					Token.ThrowIfCancellationRequested();
				}

				task = Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, Token);
			}
			return response;
		}

		public UpdateResult ProcessUpdate()
		{
			if (Directory.Exists(extractPath))
				Directory.Delete(extractPath, true);

			if (!Directory.Exists(extractPath))
				Directory.CreateDirectory(extractPath);

			using (var zip = new ZipFile(archiveFile)) {
				zip.ExtractAll(extractPath, ExtractExistingFileAction.OverwriteSilently);
			}

			if (File.Exists(Path.Combine(extractPath, "update", "Updater.exe")))
				return UpdateResult.UpdatePending;

			Import();
			return UpdateResult.OK;
		}

		public void Import()
		{
			var settings = Session.Query<Settings>().First();

			Reporter.Stage("Импорт данных");
			List<System.Tuple<string, string[]>> data;
			using (var zip = new ZipFile(archiveFile))
				data = GetDbData(zip.Select(z => z.FileName));
			Reporter.Weight(data.Count);

			var importer = new Importer(Session);
			importer.Import(data, Reporter);

			var offersImported = data.Any(t => Path.GetFileNameWithoutExtension(t.Item1).Match("offers"));
			if (offersImported) {
				RestoreOrders();
			}

			foreach (var dir in settings.DocumentDirs)
				FileHelper.CreateDirectoryRecursive(dir);

			var dirs = new List<ResultDir> {
				new ResultDir("ads", settings, rootPath, extractPath),
				new ResultDir("waybills", settings, rootPath, extractPath),
				new ResultDir("docs", settings, rootPath, extractPath),
				new ResultDir("rejects", settings, rootPath, extractPath),
			};

			var resultDirs = Directory.GetDirectories(extractPath)
				.Select(d => dirs.FirstOrDefault(r => r.Name.Match(Path.GetFileName(d))))
				.Where(d => d != null)
				.ToArray();

			resultDirs.Each(Move);
			OpenResultFiles(resultDirs);

			Directory.Delete(extractPath, true);
			WaitAndLog(Confirm(), "Подтверждение обновления");
		}

		private void RestoreOrders()
		{
			var orders = Session.Query<Order>()
				.Fetch(o => o.Address)
				.Fetch(o => o.Price)
				.Where(o => !o.Frozen)
				.ToArray();

			orders.Each(o => o.Frozen = true);
			var ids = orders.Select(o => o.Id).ToArray();
			var command = new UnfreezeCommand<Order>(ids);
			var report = (string)RunCommand(command);

			if (!String.IsNullOrEmpty(report)) {
				Results.Add(new DialogResult(new TextViewModel(report) {
					Header = "Предложения по данным позициям из заказа отсутствуют",
					DisplayName = "Не найденые позиции"
				}) {
					ShowFixed = true
				});
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
					Path.GetFullPath(Path.Combine(extractPath, t.Item2)),
					File.ReadAllLines(Path.Combine(extractPath, t.Item1))))
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
			var clientPrices = prices.Select(p => new { p.Id.PriceId, p.Id.RegionId, p.Active }).ToArray();

			var response = client.PostAsync(new Uri(BaseUri, "Main").ToString(),
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
			return Client.DeleteAsync(new Uri(BaseUri, "Main"));
		}

		public Task<HttpResponseMessage> SendLogs(HttpClient client, CancellationToken token)
		{
			var file = Path.GetTempFileName();
			var cleaner = new FileCleaner();
			cleaner.Watch(file);

			LogManager.ResetConfiguration();
			try
			{
				var logs = Directory.GetFiles(logPath, "*.log")
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

			var uri = new Uri(BaseUri, "Logs");
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
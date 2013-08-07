using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Common.Tools;
using Ionic.Zip;
using NHibernate.Linq;
using log4net.Config;
using LogManager = log4net.LogManager;

namespace AnalitF.Net.Client.Models.Commands
{
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
			Reporter.Stage("Импорт данных");
			List<System.Tuple<string, string[]>> data;
			using (var zip = new ZipFile(archiveFile))
				data = GetDbData(zip.Select(z => z.FileName));
			Reporter.Weight(data.Count);

			using (var session = AppBootstrapper.NHibernate.Factory.OpenSession()) {
				var importer = new Importer(session);
				importer.Import(data, Reporter);
				session.Flush();
			}

			var settings = new Settings();
			foreach (var dir in settings.DocumentDirs)
				FileHelper.CreateDirectoryRecursive(dir);

			var map = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase) {
				{ "ads", Path.Combine(rootPath, "ads") },
				{ "waybills", settings.MapPath("waybills") },
				{ "docs", settings.MapPath("docs") },
				{ "rejects", settings.MapPath("rejects") }
			};

			var files = Directory.GetDirectories(extractPath)
				.Select(d => Tuple.Create(d, map.GetValueOrDefault(Path.GetFileName(d))))
				.Where(t => t.Item2 != null)
				.ToArray();
			files.Each(t => Copy(t.Item1, t.Item2));

			OpenResultFiles(settings);

			Directory.Delete(extractPath, true);
			WaitAndLog(Confirm(), "Подтверждение обновления");
		}

		private void OpenResultFiles(Settings settings)
		{
			var groups = new[] {
				Tuple.Create("waybills", settings.OpenWaybills),
				Tuple.Create("rejects", settings.OpenRejects)
			};
			var files = groups.ToDictionary(g => g, g => GetFiles(settings, g.Item1));

			var openDir = files.Sum(g => g.Value.Length) > 5;

			foreach (var filesInGroup in files) {
				if (filesInGroup.Value.Length > 0) {
					if (!openDir && filesInGroup.Key.Item2) {
						Results.AddRange(filesInGroup.Value.Select(f => new OpenResult(f)));
					}
					else {
						Results.Add(new OpenResult(settings.MapPath(filesInGroup.Key.Item1)));
					}
				}
			}
		}

		private string[] GetFiles(Settings settings, string name)
		{
			var path = Path.Combine(extractPath, name);
			if (!Directory.Exists(path))
				return new string[0];

			return Directory.GetFiles(path)
				.Select(f => Path.Combine(settings.MapPath(name), Path.GetFileName(f)))
				.ToArray();
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

		private static void Copy(string source, string destination)
		{
			if (Directory.Exists(source)) {
				if (!Directory.Exists(destination)) {
					Directory.CreateDirectory(destination);
				}

				foreach (var file in Directory.GetFiles(source)) {
					File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
				}
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
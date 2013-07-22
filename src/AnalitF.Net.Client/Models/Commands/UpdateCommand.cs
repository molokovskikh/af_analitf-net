using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using Common.Tools;
using Ionic.Zip;
using NHibernate.Linq;
using log4net;
using log4net.Config;

namespace AnalitF.Net.Client.Models.Commands
{
	public class UpdateCommand : RemoteCommand
	{
		private string archiveFile;
		private string extractPath;
		private string logPath;
		private string rootPath;

		public ProgressReporter Reporter;

		public UpdateCommand(string file, string extractPath, string logPath)
		{
			this.archiveFile = file;
			this.extractPath = extractPath;
			this.logPath = logPath;
			this.rootPath = logPath;
		}

		protected override UpdateResult Execute()
		{
			Reporter = new ProgressReporter(Progress);
			Reporter.StageCount(4);

			var currentUri = new Uri(BaseUri, new Uri("Main/?reset=true", UriKind.Relative));
			var done = false;
			HttpResponseMessage response = null;

			SendPrices(Client, Token);
			var sendLogsTask = SendLogs(Client, Token);

			while (!done) {
				var request = Client.GetAsync(currentUri, HttpCompletionOption.ResponseHeadersRead, Token);
				response = request.Result;
				currentUri = new Uri(BaseUri, "Main");
				if (response.StatusCode != HttpStatusCode.OK
					&& response.StatusCode != HttpStatusCode.Accepted)
					throw new RequestException(
						String.Format("Произошла ошибка при обработке запроса, код ошибки {0} {1}",
							response.StatusCode,
							response.Content.ReadAsStringAsync().Result),
						response.StatusCode);
				Reporter.Stage("Подготовка данных");
				done = response.StatusCode == HttpStatusCode.OK;
				if (!done) {
					Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(15));
					Token.ThrowIfCancellationRequested();
				}
			}

			Reporter.Stage("Загрузка данных");
			Download(response, archiveFile, Reporter);

			var result = ProcessUpdate();
			WaitAndLog(sendLogsTask, "Отправка логов");

			return result;
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

			Directory.GetDirectories(extractPath)
				.Select(d => Tuple.Create(d, map.GetValueOrDefault(Path.GetFileName(d))))
				.Where(t => t.Item2 != null)
				.Each(t => Copy(t.Item1, t.Item2));

			Directory.Delete(extractPath, true);
			WaitAndLog(Confirm(), "Подтверждение обновления");
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
			post.ContinueWith(t => {
				stream.Dispose();
				cleaner.Dispose();
			});
			return post;
		}
	}
}
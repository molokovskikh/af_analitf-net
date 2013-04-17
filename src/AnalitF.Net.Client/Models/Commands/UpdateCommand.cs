using System;
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

		public UpdateCommand(string file, string extractPath, string logPath)
		{
			this.archiveFile = file;
			this.extractPath = extractPath;
			this.logPath = logPath;
		}

		protected override UpdateResult Execute()
		{
			var reporter = new ProgressReporter(Progress, 25);
			reporter.StageCount(4);
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
					throw new RequestException(String.Format("Произошла ошибка при обработке запроса, код ошибки {0} {1}",
							response.StatusCode,
							response.Content.ReadAsStringAsync().Result),
						response.StatusCode);
				reporter.Stage("Подготовка данных");
				done = response.StatusCode == HttpStatusCode.OK;
				if (!done) {
					Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(15));
					Token.ThrowIfCancellationRequested();
				}
			}

			reporter.Stage("Загрузка данных");
			Download(response, archiveFile, reporter);

			var result = ProcessUpdate(archiveFile, reporter);
			Log(sendLogsTask);

			return result;
		}

		public UpdateResult ProcessUpdate(string archiveFile, ProgressReporter reporter)
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

			Tasks.Import(archiveFile, reporter);
			return UpdateResult.OK;
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

			var response = client.PostAsync(new Uri(BaseUri, "Main").ToString(), new SyncRequest(clientPrices), Formatter, token).Result;
			CheckResult(response);
		}

		private void Log(Task<HttpResponseMessage> task)
		{
			if (task == null)
				return;

			if (task.IsFaulted || (task.IsCompleted && !IsOkStatusCode(task.Result.StatusCode))) {
				if (task.Exception != null)
					log.Error("Ошибка при отправке логов {0}", task.Exception);
				else
					log.ErrorFormat("Ошибка при отправке логов {0}", task.Result);
			}
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
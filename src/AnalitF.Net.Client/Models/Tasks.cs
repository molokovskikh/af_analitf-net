using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using Ionic.Zip;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Proxy;
using NHibernate.Tool.hbm2ddl;
using Newtonsoft.Json.Serialization;
using log4net;
using log4net.Config;

namespace AnalitF.Net.Client.Models
{
	public class NHibernateResolver : DefaultContractResolver
	{
		protected override List<MemberInfo> GetSerializableMembers(Type objectType)
		{
			if (typeof(INHibernateProxy).IsAssignableFrom(objectType))
				return base.GetSerializableMembers(objectType.BaseType);
			else
				return base.GetSerializableMembers(objectType);
		}
	}

	public class RequestException : Exception
	{
		public RequestException(string message, HttpStatusCode statusCode) : base(message)
		{
			StatusCode = statusCode;
		}

		public HttpStatusCode StatusCode { get; set; }
	}

	public enum UpdateResult
	{
		OK,
		UpdatePending,
	}

	public class Tasks
	{
		private static ILog log = LogManager.GetLogger(typeof(Tasks));

		public static Func<ICredentials, CancellationToken, BehaviorSubject<Progress>, UpdateResult> Update = (c, t, p) => UpdateTask(c, t, p);
		public static Func<ICredentials, CancellationToken, BehaviorSubject<Progress>, Address, UpdateResult> SendOrders = (c, t, p, a) => SendOrdersTask(c, t, p, a);

		public static Uri BaseUri;
		public static string ArchiveFile;
		public static string ExtractPath;
		public static string RootPath;

		public static UpdateResult UpdateTask(ICredentials credentials, CancellationToken cancellation, BehaviorSubject<Progress> progress)
		{
			return RemoteTask(credentials, cancellation, progress, client => {
				var currentUri = new Uri(BaseUri, new Uri("Main/?reset=true", UriKind.Relative));
				var done = false;
				HttpResponseMessage response = null;

				SendPrices(client, cancellation);
				var sendLogsTask = SendLogs(client, cancellation);

				while (!done) {
					var request = client.GetAsync(currentUri, HttpCompletionOption.ResponseHeadersRead, cancellation);
					response = request.Result;
					currentUri = new Uri(BaseUri, "Main");
					if (response.StatusCode != HttpStatusCode.OK
						&& response.StatusCode != HttpStatusCode.Accepted)
						throw new RequestException(String.Format("Произошла ошибка при обработке запроса, код ошибки {0} {1}",
								response.StatusCode,
								response.Content.ReadAsStringAsync().Result),
							response.StatusCode);
					progress.OnNext(new Progress("Подготовка данных", 0, 0));
					done = response.StatusCode == HttpStatusCode.OK;
					if (!done) {
						cancellation.WaitHandle.WaitOne(TimeSpan.FromSeconds(15));
						cancellation.ThrowIfCancellationRequested();
					}
				}

				progress.OnNext(new Progress("Подготовка данных", 100, 0));
				progress.OnNext(new Progress("Загрузка данных", 0, 33));
				using (var file = File.Create(ArchiveFile)) {
					response.Content.ReadAsStreamAsync().Result.CopyTo(file);
				}
				progress.OnNext(new Progress("Загрузка данных", 100, 33));
				progress.OnNext(new Progress("Импорт данных", 0, 66));
				var result = ProcessUpdate(ArchiveFile);
				progress.OnNext(new Progress("Импорт данных", 100, 100));

				Log(sendLogsTask);

				return result;
			});
		}

		private static void Log(Task<HttpResponseMessage> task)
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

		private static bool IsOkStatusCode(HttpStatusCode httpStatusCode)
		{
			return httpStatusCode == HttpStatusCode.OK || httpStatusCode == HttpStatusCode.NoContent;
		}

		public static Task<HttpResponseMessage> SendLogs(HttpClient client, CancellationToken token)
		{
			var file = Path.GetTempFileName();
			var cleaner = new FileCleaner();
			cleaner.Watch(file);

			LogManager.ResetConfiguration();
			try
			{
				var logs = Directory.GetFiles(RootPath, "*.log")
					.Where(f => new FileInfo(f).Length > 0)
					.ToArray();

				if (logs.Length > 0)
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

		private static void SendPrices(HttpClient client, CancellationToken token)
		{
			Price[] prices;
			using(var session = AppBootstrapper.NHibernate.Factory.OpenSession()) {
				var settings = session.Query<Settings>().First();
				var lastUpdate = settings.LastUpdate;
				prices = session.Query<Price>().Where(p => p.Timestamp > lastUpdate).ToArray();
			}

			var clientPrices = prices.Select(p => new { p.Id.PriceId, p.Id.RegionId, p.Active }).ToArray();

			var formatter = new JsonMediaTypeFormatter {
				SerializerSettings = { ContractResolver = new NHibernateResolver() }
			};
			var response = client.PostAsync(new Uri(BaseUri, "Main").ToString(), new SyncRequest(clientPrices), formatter, token).Result;

			if (response.StatusCode != HttpStatusCode.OK)
				throw new RequestException(String.Format("Произошла ошибка при обработке запроса, код ошибки {0} {1}",
						response.StatusCode,
						response.Content.ReadAsStringAsync().Result),
					response.StatusCode);
		}

		public static UpdateResult SendOrdersTask(ICredentials credentials, CancellationToken token, BehaviorSubject<Progress> progress, Address address)
		{
			var command = new SendOrders {
				BaseUri = BaseUri,
				Address = address,
				Credentials = credentials,
				Token = token,
				Progress = progress,
			};
			return command.Run();
		}

		public static UpdateResult RemoteTask(ICredentials credentials, CancellationToken cancellation, BehaviorSubject<Progress> progress, Func<HttpClient, UpdateResult> action)
		{
			var version = typeof(Tasks).Assembly.GetName().Version;

			progress.OnNext(new Progress("Соединение", 0, 0));
			var handler = new HttpClientHandler {
				Credentials = credentials,
				PreAuthenticate = true,
			};
			if (handler.Credentials == null)
				handler.UseDefaultCredentials = true;
			using (handler) {
				using (var client = new HttpClient(handler)) {
					client.DefaultRequestHeaders.Add("Version", version.ToString());
					return action(client);
				}
			}
		}

		private static UpdateResult ProcessUpdate(string archiveFile)
		{
			using (var zip = new ZipFile(archiveFile)) {
				zip.ExtractAll(ExtractPath, ExtractExistingFileAction.OverwriteSilently);
			}

			if (File.Exists(Path.Combine(ExtractPath, "update", "Updater.exe")))
				return UpdateResult.UpdatePending;

			Import(archiveFile);
			Copy(Path.Combine(ExtractPath, "ads"), Path.Combine(RootPath, "ads"));
			return UpdateResult.OK;
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

		public static UpdateResult Import(ICredentials credentials, CancellationToken token, BehaviorSubject<Progress> progress)
		{
			progress.OnNext(new Progress("Импорт данных", 0, 66));
			Import(ArchiveFile);
			progress.OnNext(new Progress("Импорт данных", 100, 100));
			return UpdateResult.OK;
		}

		public static void Import(string archiveFile)
		{
			List<System.Tuple<string, string[]>> data;
			using (var zip = new ZipFile(archiveFile))
				data = GetDbData(zip.Select(z => z.FileName));
			using (var session = AppBootstrapper.NHibernate.Factory.OpenSession()) {
				var importer = new Importer(session);
				importer.Import(data);
				session.Flush();
			}
		}

		private static List<System.Tuple<string, string[]>> GetDbData(IEnumerable<string> files)
		{
			return files.Where(f => f.EndsWith("meta.txt"))
				.Select(f => Tuple.Create(f, files.FirstOrDefault(d => Path.GetFileNameWithoutExtension(d).Match(f.Replace(".meta.txt", "")))))
				.Where(t => t.Item2 != null)
				.Select(t => Tuple.Create(
					Path.GetFullPath(Path.Combine(ExtractPath, t.Item2)),
					File.ReadAllLines(Path.Combine(ExtractPath, t.Item1))))
				.ToList();
		}

		public static void CleanDb(CancellationToken token)
		{
			var configuration = AppBootstrapper.NHibernate.Configuration;
			var factory = AppBootstrapper.NHibernate.Factory;

			var ignored = new [] { "SentOrders", "SentOrderLines", "Settings", "MarkupConfigs" };
			var tables = Tables(configuration).Except(ignored, StringComparer.InvariantCultureIgnoreCase).ToArray();

			using(var sesssion = factory.OpenSession()) {
				foreach (var table in tables) {
					sesssion.CreateSQLQuery(String.Format("TRUNCATE {0}", table))
						.ExecuteUpdate();
				}
			}
		}

		public static bool CheckAndRepairDb(CancellationToken token)
		{
			var configuration = AppBootstrapper.NHibernate.Configuration;
			var factory = AppBootstrapper.NHibernate.Factory;
			var dataPath = AppBootstrapper.DataPath;

			var tables = Tables(configuration);

			var results = new List<RepairStatus>();

			using(var session = factory.OpenSession()) {
				foreach (var table in tables) {
					token.ThrowIfCancellationRequested();

					var messages = ExecuteMaintainsQuery(String.Format("REPAIR TABLE {0} EXTENDED", table), session);
					var result = Parse(messages);
					results.Add(result);
					if (result == RepairStatus.NumberOfRowsChanged || result == RepairStatus.Ok) {
						result = Parse(ExecuteMaintainsQuery(String.Format("OPTIMIZE TABLE {0}", table), session));
						results.Add(result);
					}
					results.Add(result);
					if (result == RepairStatus.Fail) {
						log.ErrorFormat("Таблица {0} не может быть восстановлена.", table);
						log.Error(String.Format("DROP TABLE IF EXISTS {0}", table));
						session
							.CreateSQLQuery(String.Format("DROP TABLE IF EXISTS {0}", table))
							.ExecuteUpdate();
						//если заголовок таблицы поврежден то drop table не даст результатов
						//файлы останутся а при попытке создать таблицу будет ошибка
						//нужно удалить файлы
						Directory.GetFiles(dataPath, table + ".*").Each(File.Delete);
					}
				}
			}

			new SanityCheck(dataPath).Check(true);

			return results.All(r => r == RepairStatus.Ok);
		}

		private static List<string[]> ExecuteMaintainsQuery(string sql, ISession session)
		{
			log.ErrorFormat(sql);
			var messages = session
				.CreateSQLQuery(sql)
				.List<object[]>()
				.Select(m => m.Select(v => v.ToString()).ToArray())
				.ToList();
			log.Error(messages.Implode(s => s.Implode(), System.Environment.NewLine));
			return messages;
		}

		private static IEnumerable<string> Tables(Configuration configuration)
		{
			var dialect = NHibernate.Dialect.Dialect.GetDialect(configuration.Properties);
			var tables = configuration.CreateMappings(dialect).IterateTables.Select(t => t.Name);
			return tables;
		}

		private static RepairStatus Parse(IList<string[]> messages)
		{
			//не ведомо что случилось, но нужно верить в лучшее
			if (!messages.Any())
				return RepairStatus.Ok;

			var notExist = messages.Where(m => m[2].Match("error")).Any(m => Regex.Match(m[3], "Table .+ doesn't exist").Success);
			if (notExist)
				return RepairStatus.NotExist;

			var ok = messages.Where(m => m[2].Match("status")).Any(m => m[3].Match("OK"));
			if (ok) {
				var rowsChanged = messages.Where(m => m[2].Match("error")).Any(m => m[3].StartsWith("Number of rows changed"));
				if (rowsChanged)
					return RepairStatus.NumberOfRowsChanged;
				return RepairStatus.Ok;
			}
			return RepairStatus.Fail;
		}

		public enum RepairStatus
		{
			Ok,
			NumberOfRowsChanged,
			NotExist,
			Fail
		}
	}
}
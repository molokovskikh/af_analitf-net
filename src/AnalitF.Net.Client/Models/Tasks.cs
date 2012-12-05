using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using Ionic.Zip;
using NHibernate.Linq;
using NHibernate.Proxy;
using Newtonsoft.Json.Serialization;

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
		public static Func<ICredentials, CancellationToken, BehaviorSubject<Progress>, Task<UpdateResult>> Update = (c, t, p) => UpdateTask(c, t, p);
		public static Func<ICredentials, CancellationToken, BehaviorSubject<Progress>, Task<UpdateResult>> SendOrders = (c, t, p) => SendOrdersTask(c, t, p);

		public static Uri Uri;
		public static string ArchiveFile;
		public static string ExtractPath;

		public static Task<UpdateResult> UpdateTask(ICredentials credentials, CancellationToken cancellation, BehaviorSubject<Progress> progress)
		{
			return RemoteTask(credentials, cancellation, progress, client => {
				var currentUri = new UriBuilder(Uri) {
					Query = "reset=true"
				}.Uri;
				var done = false;
				HttpResponseMessage response = null;

				while (!done) {
					var request = client.GetAsync(currentUri, HttpCompletionOption.ResponseHeadersRead, cancellation);
					currentUri = Uri;
					response = request.Result;
					if (response.StatusCode != HttpStatusCode.OK
						&& response.StatusCode != HttpStatusCode.Accepted)
						throw new RequestException(String.Format("Произошла ошибка при обработке запроса, код ошибки {0}", response.StatusCode),
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
				var result = Import(ArchiveFile);
				progress.OnNext(new Progress("Импорт данных", 100, 100));
				return result;
			});
		}

		public static Task<UpdateResult> SendOrdersTask(ICredentials credentials, CancellationToken token, BehaviorSubject<Progress> progress)
		{
			return RemoteTask(credentials, token, progress, client => {
				progress.OnNext(new Progress("Соединение", 100, 0));
				progress.OnNext(new Progress("Отправка заказов", 0, 50));
				using (var session = AppBootstrapper.NHibernate.Factory.OpenSession())
				using (var transaction = session.BeginTransaction()) {
					var orders = session.Query<Order>().ToList();
					var clientOrders = orders.Select(o => o.ToClientOrder()).ToArray();

					var formatter = new JsonMediaTypeFormatter {
						SerializerSettings = { ContractResolver = new NHibernateResolver() }
					};
					var response = client.PostAsync(Uri.ToString(), clientOrders, formatter, token).Result;

					if (response.StatusCode != HttpStatusCode.OK)
						throw new RequestException(String.Format("Произошла ошибка при обработке запроса, код ошибки {0}", response.StatusCode),
							response.StatusCode);

					foreach (var order in orders)
						session.Save(new SentOrder(order));

					foreach (var order in orders)
						session.Delete(order);

					transaction.Commit();
				}
				progress.OnNext(new Progress("Отправка заказов", 100, 100));
				return UpdateResult.OK;
			});
		}

		private static Task<UpdateResult> RemoteTask(ICredentials credentials, CancellationToken cancellation, BehaviorSubject<Progress> progress, Func<HttpClient, UpdateResult> action)
		{
			return new Task<UpdateResult>(() => {
				var version = typeof(Tasks).Assembly.GetName().Version;

				progress.OnNext(new Progress("Соединение", 0, 0));
				var handler = new HttpClientHandler {
					Credentials = credentials,
					PreAuthenticate = true
				};
				if (handler.Credentials == null)
					handler.UseDefaultCredentials = true;
				using (handler) {
					using (var client = new HttpClient(handler)) {
						client.DefaultRequestHeaders.Add("Version", version.ToString());
						return action(client);
					}
				}
			}, cancellation);
		}

		private static UpdateResult Import(string archiveFile)
		{
			List<System.Tuple<string, string[]>> data;
			using (var zip = new ZipFile(archiveFile)) {
				zip.ExtractAll(ExtractPath, ExtractExistingFileAction.OverwriteSilently);
				data = GetDbData(zip.Select(z => z.FileName));
			}

			if (File.Exists(Path.Combine(ExtractPath, "update", "Updater.exe")))
				return UpdateResult.UpdatePending;

			using (var session = AppBootstrapper.NHibernate.Factory.OpenSession()) {
				var importer = new Importer(session);
				importer.Import(data);
			}
			return UpdateResult.OK;
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
	}
}
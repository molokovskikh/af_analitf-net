using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;
using LogManager = log4net.LogManager;

namespace AnalitF.Net.Client.Models.Commands
{
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
		//данные в базе изменились их нужно перезагрузить и показать сообщение об успехе
		OK,
		//данные в базе изменились их надо перезагрузить но не нужно показывать уведомление
		SilentOk,
		//данные в базе не изменились не надо перезагружать их
		NotReload,
		//данные в базе не изменились тк получил обновление бинарников, надо запустить процесс обновления
		UpdatePending,
	}

	public abstract class RemoteCommand : BaseCommand
	{
		protected HttpClient Client;
		protected JsonMediaTypeFormatter Formatter;

		public string ClientToke;
		public ICredentials Credentials;
		public IWebProxy Proxy;
		public string RasConnection;

		public string ErrorMessage;
		public string SuccessMessage;
		public List<IResult> Results = new List<IResult>();
		protected TimeSpan? RequestInterval;

		protected RemoteCommand()
		{
			log = LogManager.GetLogger(GetType());

			Formatter = new JsonMediaTypeFormatter {
				SerializerSettings = JsonHelper.SerializerSettings()
			};
		}

		protected abstract UpdateResult Execute();

		public virtual UpdateResult Run()
		{
			return Process(Execute);
		}

		public T Process<T>(Func<T> method)
		{
			try {
				return RemoteTask(Progress, c => {
					Client = c;
					var factory = Factory;
					using (Session = factory.OpenSession())
					using (StatelessSession = factory.OpenStatelessSession())
					using (var transaction = Session.BeginTransaction()) {
						var result = method();
						transaction.Commit();
						return result;
					}
				});
			}
			finally {
				Session = null;
				StatelessSession = null;
				Client = null;
			}
		}

		private T RemoteTask<T>(BehaviorSubject<Progress> progress,
			Func<HttpClient, T> action)
		{
			var version = typeof(AppBootstrapper).Assembly.GetName().Version;
			progress.OnNext(new Progress("Соединение", 0, 0));

			var handler = new HttpClientHandler {
				Credentials = Credentials,
				PreAuthenticate = true,
				Proxy = Proxy,
			};
			if (handler.Credentials == null)
				handler.UseDefaultCredentials = true;

			using (var ras = new RasHelper(RasConnection))
			using (handler)
			using (var client = new HttpClient(handler)) {
				client.BaseAddress = Config.BaseUrl;
				ras.Open();
				client.DefaultRequestHeaders.Add("Version", version.ToString());
				client.DefaultRequestHeaders.Add("Client-Token", ClientToke);
				//признак по которому запросы можно объединить, нужно что бы в интерфейсе связать лог и запрос
				client.DefaultRequestHeaders.Add("Request-Token", Guid.NewGuid().ToString());
				try {
					client.DefaultRequestHeaders.Add("OS-Version", Environment.OSVersion.VersionString);
				}
				catch (Exception) { }
				return action(client);
			}
		}

		protected void CheckResult(HttpResponseMessage response)
		{
			if (!IsOkStatusCode(response.StatusCode)) {
				//если включена отладка попробуем собрать дополнительную отладочную информацию
				if (log.IsDebugEnabled) {
					try {
						var content = response.Content.ReadAsStringAsync().Result;
						log.DebugFormat("Ошибка {1} при обработке запроса {0}, {2}",
							response.RequestMessage, response, content);
					}
					catch(Exception e) {
						log.Warn(String.Format("Не удалось получить отладочную" +
							" информацию об ошибке при обработке запроса {0}",
							response.RequestMessage), e);
					}
				}
				throw new RequestException(String.Format("Произошла ошибка при обработке запроса, код ошибки {0}",
						response.StatusCode),
					response.StatusCode);
			}
		}

		protected static bool IsOkStatusCode(HttpStatusCode httpStatusCode)
		{
			return httpStatusCode == HttpStatusCode.OK || httpStatusCode == HttpStatusCode.NoContent;
		}

		public void Configure(Settings value, Config.Config config, CancellationToken token)
		{
			Credentials = value.GetCredential();
			Proxy = value.GetProxy();
			ClientToke = value.GetClientToken();
			if (value.UseRas) {
				RasConnection = value.RasConnection;
			}
			Config = config;
			Token = token;
		}

		protected HttpResponseMessage Wait(string statusCheckUrl, Task<HttpResponseMessage> task, ref uint requestId)
		{
			HttpResponseMessage response = null;
			try {
				while (true) {
					if (response != null)
						response.Dispose();

					response = task.Result;
					if (response.StatusCode == HttpStatusCode.OK)
						return response;

					if (response.StatusCode == HttpStatusCode.Accepted) {
						Reporter.Stage("Подготовка данных");
						if (requestId == 0
							&& response.Content.Headers.ContentType != null
							&& response.Content.Headers.ContentType.MediaType == "application/json") {
							requestId = ((dynamic)response.Content.ReadAsAsync<object>().Result).RequestId;
						}
						Token.WaitHandle.WaitOne(RequestInterval ?? Config.RequestInterval);
						Token.ThrowIfCancellationRequested();
						task = Client.GetAsync(statusCheckUrl, HttpCompletionOption.ResponseHeadersRead, Token);
						continue;
					}

					if (response.StatusCode == HttpStatusCode.InternalServerError
						&& response.Content.Headers.ContentType != null) {
						if (response.Content.Headers.ContentType.MediaType == "text/plain")
							throw new EndUserError(response.Content.ReadAsStringAsync().Result);
#if DEBUG
						if (response.Content.Headers.ContentType.MediaType == "application/json")
							throw new Exception(response.Content.ReadAsAsync<DebugServerError>().Result.ToString());
#endif
					}
					throw new RequestException(
						String.Format("Произошла ошибка при обработке запроса, код ошибки {0} {1}",
							response.StatusCode,
							response.Content.ReadAsStringAsync().Result),
						response.StatusCode);
				}
			}
			catch(Exception) {
				if (response != null)
					response.Dispose();
				throw;
			}
		}
	}
}
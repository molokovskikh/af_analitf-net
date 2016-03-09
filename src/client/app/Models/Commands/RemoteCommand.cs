using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Handlers;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;
using Common.Tools;
using NHibernate;
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
		protected ProgressMessageHandler HttpProgress;
		protected JsonMediaTypeFormatter Formatter;
		protected TimeSpan? RequestInterval;

		public string ErrorMessage;
		public string SuccessMessage;
		public List<IResult> Results = new List<IResult>();
		public Settings Settings;
		public HttpClientHandler Handler;
		public HttpClient Client;

		static RemoteCommand()
		{
			//у нас нет валидного сертификата игнорируем проверку
			ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;
		}

		protected RemoteCommand()
		{
			Log = LogManager.GetLogger(GetType());

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
				Progress.OnNext(new Progress("Соединение", 0, 0));
				using (Session = Factory.OpenSession())
				using (var transaction = Session.BeginTransaction())
				using (StatelessSession = Factory.OpenStatelessSession(Session.Connection)) {
					var result = method();
					transaction.Commit();
					return result;
				}
			}
			finally {
				Session = null;
				StatelessSession = null;
			}
		}

		protected void CheckResult(Task<HttpResponseMessage> task)
		{
			var response = task.Result;
			if (!IsOkStatusCode(response.StatusCode)) {
				//если включена отладка попробуем собрать дополнительную отладочную информацию
				if (Log.IsDebugEnabled) {
					try {
						var content = response.Content.ReadAsStringAsync().Result;
						Log.DebugFormat("Ошибка {1} при обработке запроса {0}, {2}", response.RequestMessage, response, content);
					}
					catch(Exception e) {
						Log.Warn("Не удалось получить отладочную информацию" +
							$" об ошибке при обработке запроса {response.RequestMessage}", e);
					}
				}
				throw new RequestException($"Произошла ошибка при обработке запроса, код ошибки {response.StatusCode}",
					response.StatusCode);
			}
		}

		protected static bool IsOkStatusCode(HttpStatusCode? code)
		{
			//запрос не производился
			if (code == null)
				return true;
			return code == HttpStatusCode.OK || code == HttpStatusCode.NoContent;
		}

		public virtual void Configure(Settings value, Config.Config config,
			CancellationToken token = default(CancellationToken))
		{
			Config = config;
			Token = token;
			Settings = value;
			if (Client != null) {
				Client.Dispose();
				Disposable.Remove(Client);
			}
			if (HttpProgress != null) {
				HttpProgress.HttpReceiveProgress -= ReceiveProgress;
			}
			Client = Settings.GetHttpClient(Config, ref HttpProgress, ref Handler);
			Disposable.Add(Client);
			HttpProgress.HttpReceiveProgress += ReceiveProgress;
		}

		protected virtual void ReceiveProgress(object sender, HttpProgressEventArgs args)
		{
		}

		protected HttpResponseMessage Wait(string statusCheckUrl, Task<HttpResponseMessage> task, ref uint requestId)
		{
			HttpResponseMessage response = null;
			try {
				while (true) {
					response?.Dispose();

					response = task.Result;

					IEnumerable<string> headers;
					if (response.Headers.TryGetValues("Request-Id", out headers))
						requestId = SafeConvert.ToUInt32(headers.FirstOrDefault());
					if (response.IsSuccessStatusCode)
						Util.Assert(requestId != 0, "Request-Id должен быть указан");

					if (response.StatusCode == HttpStatusCode.OK)
						return response;
					if (response.StatusCode == HttpStatusCode.Accepted) {
						Reporter.Stage("Подготовка данных");
						Token.WaitHandle.WaitOne(RequestInterval ?? Config.RequestInterval);
						Token.ThrowIfCancellationRequested();
						task = Client.GetAsync(statusCheckUrl, HttpCompletionOption.ResponseHeadersRead, Token);
						continue;
					}
					//если это запрет то сервер может в теле передать причину
					if (response.StatusCode == HttpStatusCode.Forbidden
						&& response.Content.Headers.ContentType?.MediaType == "text/plain") {
						throw new EndUserError(response.Content.ReadAsStringAsync().Result);
					}

					if (response.StatusCode == HttpStatusCode.InternalServerError) {
						if (response.Content.Headers.ContentType?.MediaType == "text/plain")
							throw new EndUserError(response.Content.ReadAsStringAsync().Result);
#if DEBUG
						if (response.Content.Headers.ContentType?.MediaType == "application/json")
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
				response?.Dispose();
				throw;
			}
		}

		public async Task<object> Check(HttpClient client, string url)
		{
			try {
				var response = await client.GetAsync(url + "Status");
				response.EnsureSuccessStatusCode();
				var content = await response.Content.ReadAsStringAsync();
				//в ответ должен прийти номер версии
				return Version.Parse(content);
			}
			catch(Exception e) {
				return e;
			}
		}

		public Uri ConfigureHttp()
		{
			try {
				var urls = Config.AltUri;
				if (String.IsNullOrEmpty(urls))
					return null;
				ProgressMessageHandler progress = null;
				HttpClientHandler handler = null;
				//нужен еще один клиент тк BaseAddress после первого запроса менять нельзя
				using (var client = Settings.GetHttpClient(Config, ref progress, ref handler)) {
					Log.Info($"Поиск доступного хоста, выбор из {urls}");
					var requests = urls.Split(',').Select(x => Tuple.Create(x, Check(client, x))).ToArray();
					Task.WaitAll(requests.Select(x => x.Item2).ToArray(), 10*1000, Token);

					var responded = requests
						.Where(x => x.Item2.IsCompleted && x.Item2.Result is Version)
						.ToArray();
					var faulted = requests
						.Where(x => x.Item2.IsCompleted && x.Item2.Result is Exception)
						.ToArray();
					var cancelled = requests.Except(responded).Except(faulted).ToArray();
					if (responded.Length > 0)
						Log.Info($"Ответили {responded.Implode(x => x.Item1)}");
					if (faulted.Length > 0)
						Log.Info($"Недоступны {faulted.Implode(x => x.Item1)}");
					if (cancelled.Length > 0)
						Log.Info($"Нет ответа {cancelled.Implode(x => x.Item1)}");

					if (responded.Length > 0) {
						var host = new Uri(responded[new Random().Next(responded.Length)].Item1);
						Log.Info($"Выбран для обмена данными {host}");
						return host;
					}
				}
			}
			catch(Exception e) {
				Log.Error("Не удалось определить хост для обмена данными", e);
			}
			return null;
		}
	}
}
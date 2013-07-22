using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reactive.Subjects;
using System.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels;
using NHibernate;
using log4net;

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
		OK,
		UpdatePending,
	}

	public abstract class RemoteCommand
	{
		protected ILog log;
		public Uri BaseUri;
		protected HttpClient Client;
		protected JsonMediaTypeFormatter Formatter;
		public ICredentials Credentials;
		public CancellationToken Token;

		public BehaviorSubject<Progress> Progress;

		protected ISession Session;

		protected RemoteCommand()
		{
			log = LogManager.GetLogger(GetType());

			Formatter = new JsonMediaTypeFormatter {
				SerializerSettings = JsonHelper.SerializerSettings()
			};
		}

		protected abstract UpdateResult Execute();

		public UpdateResult Run()
		{
			return Process(Execute);
		}

		public T Process<T>(Func<T> method)
		{
			try {
				return RemoteTask(Credentials, Token, Progress, c => {
					Client = c;
					using (Session = AppBootstrapper.NHibernate.Factory.OpenSession())
					using (var transaction = Session.BeginTransaction()) {
						var result = method();
						transaction.Commit();
						return result;
					}
				});
			}
			finally {
				Session = null;
				Client = null;
			}
		}

		public static T RemoteTask<T>(ICredentials credentials, CancellationToken cancellation,
			BehaviorSubject<Progress> progress,
			Func<HttpClient, T> action)
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

		public void CheckResult(HttpResponseMessage response)
		{
			if (!IsOkStatusCode(response.StatusCode)) {
				//если включена отладка попробуем собрать дополнительную отладочную информацию
				if (log.IsDebugEnabled) {
					try {
						var content = response.Content.ReadAsStringAsync().Result;
						log.DebugFormat("Ошибка {1} при обработке запроса {0}, {2}", response.RequestMessage, response, content);
					}
					catch(Exception e) {
						log.Warn(String.Format("Не удалось получить отладочную информацию об ошибке при обработке запроса {0}", response.RequestMessage), e);
					}
				}
				throw new RequestException(String.Format("Произошла ошибка при обработке запроса, код ошибки {0}",
						response.StatusCode),
					response.StatusCode);
			}
		}

		public static bool IsOkStatusCode(HttpStatusCode httpStatusCode)
		{
			return httpStatusCode == HttpStatusCode.OK || httpStatusCode == HttpStatusCode.NoContent;
		}
	}
}
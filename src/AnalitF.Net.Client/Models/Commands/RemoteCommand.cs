using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reactive.Subjects;
using System.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using NHibernate;
using ILog = log4net.ILog;
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
		OK,
		Other,
		UpdatePending,
	}

	public abstract class RemoteCommand : BaseCommand
	{
		protected HttpClient Client;
		protected JsonMediaTypeFormatter Formatter;

		public ICredentials Credentials;

		public string ErrorMessage;
		public string SuccessMessage;
		public List<IResult> Results = new List<IResult>();

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
				return RemoteTask(Credentials, Token, Progress, c => {
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
				Client = null;
			}
		}

		public static T RemoteTask<T>(ICredentials credentials, CancellationToken cancellation,
			BehaviorSubject<Progress> progress,
			Func<HttpClient, T> action)
		{
			var version = typeof(AppBootstrapper).Assembly.GetName().Version;

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

		public static bool IsOkStatusCode(HttpStatusCode httpStatusCode)
		{
			return httpStatusCode == HttpStatusCode.OK || httpStatusCode == HttpStatusCode.NoContent;
		}
	}
}
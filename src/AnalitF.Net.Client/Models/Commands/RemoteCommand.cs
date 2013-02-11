using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reactive.Subjects;
using System.Threading;
using AnalitF.Net.Client.ViewModels;
using NHibernate;

namespace AnalitF.Net.Client.Models.Commands
{
	public abstract class RemoteCommand
	{
		public Uri BaseUri;
		protected HttpClient Client;
		protected JsonMediaTypeFormatter Formatter;
		public ICredentials Credentials;
		public CancellationToken Token;

		public BehaviorSubject<Progress> Progress;

		protected ISession Session;

		protected RemoteCommand()
		{
			Formatter = new JsonMediaTypeFormatter {
				SerializerSettings = { ContractResolver = new NHibernateResolver() }
			};
		}

		protected abstract UpdateResult Execute();

		public UpdateResult Run()
		{
			try
			{
				Tasks.RemoteTask(Credentials, Token, Progress, c => {
					Client = c;
					using (Session = AppBootstrapper.NHibernate.Factory.OpenSession())
					using (var transaction = Session.BeginTransaction()) {
						var result = Execute();
						transaction.Commit();
						return result;
					}
				});
			}
			finally {
				Session = null;
				Client = null;
			}
			throw new Exception("Задача не запущена");
		}

		public static void CheckResult(HttpResponseMessage response)
		{
			if (response.StatusCode != HttpStatusCode.OK)
				throw new RequestException(String.Format("Произошла ошибка при обработке запроса, код ошибки {0}", response.StatusCode),
					response.StatusCode);
		}
	}
}
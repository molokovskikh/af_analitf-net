using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Service.Controllers;
using AnalitF.Net.Service.Models;
using log4net;
using NHibernate;

namespace AnalitF.Net.Service.Helpers
{
	public class RequestHelper
	{
		public static ILog log = LogManager.GetLogger(typeof(RequestHelper));

		public static Version GetVersion(HttpRequestMessage request)
		{
			var headers = request.Headers;
			var version = new Version();
			IEnumerable<string> header;
			if (headers.TryGetValues("Version", out header)) {
				Version.TryParse(header.FirstOrDefault(), out version);
			}
			return version;
		}

		public static Task StartJob(ISession session,
			RequestLog existsJob,
			Config.Config config,
			ISessionFactory sessionFactory,
			Action<ISession, Config.Config, RequestLog> cmd)
		{
			var principal = Thread.CurrentPrincipal;
			session.Save(existsJob);
			session.Transaction.Commit();
			var jobId = existsJob.Id;

			var task = new Task(() => {
				try {
					Thread.CurrentPrincipal = principal;
					using (var logSession = sessionFactory.OpenSession())
					using (var logTransaction = logSession.BeginTransaction()) {
						var job = logSession.Load<RequestLog>(jobId);
						try {
							using(var cmdSession = sessionFactory.OpenSession())
							using(var cmdTransaction = cmdSession.BeginTransaction()) {
								cmd(cmdSession, config, job);
								cmdTransaction.Commit();
							}
						}
						catch(Exception e) {
							log.Error(String.Format("Произошла ошибка при обработке запроса {0}", jobId), e);
							job.Faulted(e);
						}
						finally {
							job.Completed();
							logSession.Save(job);
							logSession.Flush();
							logTransaction.Commit();
						}
					}
				}
				catch(Exception e) {
					log.Error(String.Format("Произошла ошибка при обработке запроса {0}", jobId), e);
				}
			});
			task.Start();
			return task;
		}
	}
}
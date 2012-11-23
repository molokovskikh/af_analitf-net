using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using AnalitF.Net.Models;
using Common.Models;
using Common.Tools;
using Ionic.Zip;
using NHibernate;
using NHibernate.Linq;
using log4net;

namespace AnalitF.Net.Controllers
{
	public class MainController : ApiController
	{
		public static ILog log = LogManager.GetLogger(typeof(MainController));

		public ISession Session { get; set; }
		public User CurrentUser { get; set; }

		public HttpResponseMessage Get(bool reset = false)
		{
			var existsJob = Session.Query<RequestLog>()
				.OrderByDescending(j => j.CreatedOn)
				.FirstOrDefault(j => j.User == CurrentUser);

			if (existsJob != null) {
				if (existsJob.IsStale)
					existsJob = null;
				else if (reset)
					existsJob = null;
			}


			if (existsJob == null) {
				var version = new Version();
				IEnumerable<string> headers;
				if (Request.Headers.TryGetValues("Version", out headers)) {
					Version.TryParse(headers.FirstOrDefault(), out version);
				}

				existsJob  = new RequestLog(CurrentUser, version);
				Session.Save(existsJob);
				Session.Transaction.Commit();

				StartJob(existsJob.Id, Session.SessionFactory);
			}

			if (!existsJob.IsCompleted)
				return new HttpResponseMessage(HttpStatusCode.Accepted);

			return new HttpResponseMessage(HttpStatusCode.OK) {
				Content = new StreamContent(existsJob.GetResult(FileHelper.MakeRooted(ConfigurationManager.AppSettings["ResultPath"])))
			};
		}

		public static Task StartJob(uint jobId, ISessionFactory sessionFactory)
		{
			var task = new Task(() => {
				try {
					using (var session = sessionFactory.OpenSession()) {
						var job = session.Load<RequestLog>(jobId);
						try {
							var exporter = new Exporter(session, job.User.Id, job.Version) {
								Prefix = job.Id.ToString(),
								ExportPath = FileHelper.MakeRooted(ConfigurationManager.AppSettings["ExportPath"]),
								ResultPath = FileHelper.MakeRooted(ConfigurationManager.AppSettings["ResultPath"]),
								UpdatePath = FileHelper.MakeRooted(ConfigurationManager.AppSettings["UpdatePath"])
							};
							using (exporter) {
								exporter.ExportCompressed(job.OutputFile);
							}
						}
						catch(Exception e) {
							log.Error(String.Format("Произошла ошибка при обработке запроса {0}", jobId), e);
							job.IsFaulted = true;
							job.Error = e.ToString();
						}
						finally {
							job.IsCompleted = true;
							session.Save(job);
							session.Flush();
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

		public void Post()
		{
		}
	}
}

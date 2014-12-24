using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using AnalitF.Net.Service.Helpers;
using AnalitF.Net.Service.Models;
using Common.Models;
using log4net;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Service.Controllers
{
	//нужен тк MainController уже определяет get но с параметрами
	public class JobController2 : JobController
	{
		public HttpResponseMessage Get()
		{
			var existsJob = TryFindJob(false, GetType().Name);
			if (existsJob == null)
				return new HttpResponseMessage(HttpStatusCode.Accepted);
			return existsJob.ToResult(Config);
		}
	}

	public class JobController : ApiController
	{
		protected static ILog Log;

		public Config.Config Config;
		public ISession Session { get; set; }
		public User CurrentUser { get; set; }
		//для тестирования
		public Task Task;

		public JobController()
		{
			Log = LogManager.GetLogger(GetType());
		}

		protected RequestLog TryFindJob(bool reset, string type = null)
		{
			var query = Session.Query<RequestLog>();
			if (type != null)
				query = query.Where(j => j.UpdateType == type);

			var existsJob = query.OrderByDescending(j => j.CreatedOn)
				.FirstOrDefault(j => j.User == CurrentUser);

			if (existsJob != null) {
				if (existsJob.IsStale)
					existsJob = null;
				else if (reset)
					existsJob = null;
			}
			return existsJob;
		}

		protected HttpResponseMessage StartJob(Action<ISession, Config.Config, RequestLog> cmd)
		{
			var existsJob = new RequestLog(CurrentUser, Request, GetType().Name);
			Task = RequestHelper.StartJob(Session, existsJob, Config, Session.SessionFactory, cmd);
			return existsJob.ToResult(Config);
		}
	}
}
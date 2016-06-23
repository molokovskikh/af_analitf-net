using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
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
			var existsJob = TryFindJob(GetType().Name);
			if (existsJob == null)
				return new HttpResponseMessage(HttpStatusCode.Accepted);
			return existsJob.ToResult(Request, Config);
		}
	}

	public class JobController : ApiController
	{
		protected static ILog Log;

		public Config.Config Config;
		public ISession Session { get; set; }
		public User CurrentUser { get; set; }

		public JobController()
		{
			Log = LogManager.GetLogger(GetType());
		}

		protected RequestLog TryFindJob(string updateType)
		{
			var existsJob = Session.Query<RequestLog>()
				.Where(j => j.UpdateType == updateType && !j.IsConfirmed && j.User == CurrentUser)
				.OrderByDescending(j => j.CreatedOn)
				.FirstOrDefault();
			if (existsJob?.GetIsStale(TimeSpan.FromMinutes(30)) == true)
				return null;
			return existsJob;
		}

		protected HttpResponseMessage StartJob(Action<ISession, Config.Config, RequestLog> cmd)
		{
			var existsJob = new RequestLog(CurrentUser, Request, GetType().Name);
			existsJob.StartJob(Session, (x, y) => cmd(x, Config, y));
			return existsJob.ToResult(Request, Config);
		}
	}
}
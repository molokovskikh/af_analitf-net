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

		protected RequestLog TryFindJob(bool reset, string updateType)
		{
			if (reset) {
				//если данные уже готовятся нет смысла делать это повторно тк это все равное не будет работать только
				//создаст дополнительную нагрузку на базу данных
				var inProcess = Session.Query<RequestLog>()
					.Where(j => j.UpdateType == updateType && !j.IsCompleted
						&& j.User == CurrentUser
						&& j.CreatedOn > DateTime.Now.AddMinutes(-10))
					.OrderByDescending(j => j.CreatedOn)
					.ToList();
				return inProcess.FirstOrDefault();
			} else {
				var existsJob = Session.Query<RequestLog>()
					.Where(j => j.UpdateType == updateType && !j.IsConfirmed && j.User == CurrentUser)
					.OrderByDescending(j => j.CreatedOn)
					.FirstOrDefault();
				if (existsJob?.IsStale == true)
					return null;
				return existsJob;
			}
		}

		protected HttpResponseMessage StartJob(Action<ISession, Config.Config, RequestLog> cmd)
		{
			var existsJob = new RequestLog(CurrentUser, Request, GetType().Name);
			Task = existsJob.StartJob(Session, Config, cmd);
			return existsJob.ToResult(Config);
		}
	}
}
using System.Linq;
using System.Web.Http;
using AnalitF.Net.Service.Models;
using Common.Models;
using log4net;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Service.Controllers
{
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
	}
}
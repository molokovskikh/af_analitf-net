using System;
using System.Web.Http.Filters;
using log4net;
using NHibernate;

namespace AnalitF.Net.Service.Filters
{
	public class LogAccess : ActionFilterAttribute
	{
		private ILog log = LogManager.GetLogger(typeof(LogAccess));

		public override void OnActionExecuted(HttpActionExecutedContext context)
		{
			if (context.Exception != null)
				return;
			var session = (ISession)((dynamic)context.ActionContext.ControllerContext.Controller).Session;
			if (session == null)
				return;
			var user = ((dynamic)context.ActionContext.ControllerContext.Controller).CurrentUser;
			if (user == null)
				return;
			var userId = (object)user.Id;

			try {
				session
					.CreateSQLQuery("update Logs.AuthorizationDates set AFNetTime = now() where UserId = :userId")
					.SetParameter("userId", userId)
					.ExecuteUpdate();
			}
			catch(Exception e) {
				log.Error("Не удалось обновить время доступа", e);
			}
		}
	}
}
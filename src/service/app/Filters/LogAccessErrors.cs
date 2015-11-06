using System;
using System.Web.Http;
using System.Web.Http.Filters;
using AnalitF.Net.Service.Models;
using Common.Web.Service.Filters;
using log4net;
using NHibernate;

namespace AnalitF.Net.Service.Filters
{
	public class LogAccessErrors : ExceptionFilterAttribute
	{
		private ILog log = LogManager.GetLogger(typeof(LogAccessErrors));

		public override void OnException(HttpActionExecutedContext actionContext)
		{
			var context = actionContext.ActionContext.ControllerContext;
			var access = actionContext.Exception as AccessException;
			if (access == null)
				return;

			var factory = (ISessionFactory)context.Configuration.Properties[typeof(ISessionFactory)];
			try {
				using (var logSession = factory.OpenSession())
				using (var trx = logSession.BeginTransaction()) {
					var requestLog = new RequestLog(access.User, actionContext.Request, context.Controller.GetType().Name);
					requestLog.Faulted(new ExporterException(access.InternalMessage, ErrorType.AccessDenied));
					logSession.Save(requestLog);
					trx.Commit();
				}
			} catch(Exception e) {
				log.Error("Ошибка протоколирования", e);
			}
		}
	}
}
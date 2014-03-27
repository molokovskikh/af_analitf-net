using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Common.Tools;
using NHibernate.Util;

namespace AnalitF.Net.Service.Filters
{
	public class DebugFilter : ActionFilterAttribute
	{
		public override void OnActionExecuting(HttpActionContext actionContext)
		{
			IEnumerable<string> headers;
			if (actionContext.Request.Headers.TryGetValues("debug-timeout", out headers))
				Thread.Sleep((int)(SafeConvert.ToDecimal(headers.FirstOrDefault()) * 1000));
			if (actionContext.Request.Headers.TryGetValues("debug-fault", out headers) && headers.Any())
				throw new Exception("Тестовое исключение");
			base.OnActionExecuting(actionContext);
		}
	}
}
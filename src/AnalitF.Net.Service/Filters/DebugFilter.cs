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
				Thread.Sleep(SafeConvert.ToInt32(headers.FirstOrDefault()).GetValueOrDefault());
			base.OnActionExecuting(actionContext);
		}
	}
}
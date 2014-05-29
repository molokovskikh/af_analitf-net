using System.Threading;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using log4net;

namespace AnalitF.Net.Service.Filters
{
	public class ConfigFilter : ActionFilterAttribute
	{
		public override void OnActionExecuting(HttpActionContext context)
		{
			ThreadContext.Properties["username"] = Thread.CurrentPrincipal.Identity.Name;
			var controllerContext = context.ControllerContext;
			var config = controllerContext.Configuration.Properties["config"];
			if (config == null)
				return;

			var field = controllerContext.Controller.GetType().GetField("Config");
			if (field == null)
				return;
			field.SetValue(controllerContext.Controller, config);
		}
	}
}
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace AnalitF.Net.Service.Filters
{
	public class ConfigFilter : ActionFilterAttribute
	{
		public override void OnActionExecuting(HttpActionContext context)
		{
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
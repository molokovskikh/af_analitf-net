using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace AnalitF.Net
{
	public class Application : HttpApplication
	{
		public static void RegisterRoutes(RouteCollection routes)
		{
			routes.MapHttpRoute(
				name: "DefaultApi",
				routeTemplate: "{controller}/{id}",
				defaults: new { id = RouteParameter.Optional });
		}

		protected void Application_Start()
		{
			RegisterRoutes(RouteTable.Routes);
		}
	}
}

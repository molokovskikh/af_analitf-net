using System.Web;
using System.Web.Http;
using System.Web.Routing;
using AnalitF.Net.Models;
using Common.Web.Service.Filters;
using NHibernate;
using log4net.Config;

namespace AnalitF.Net
{
	public class Application : HttpApplication
	{
		public static ISessionFactory SessionFactory;

		protected void Application_Start()
		{
			XmlConfigurator.Configure();

			var nhibernate = new Config.Initializers.NHibernate();
			nhibernate.Init();
			SessionFactory = nhibernate.Factory;

			Configure(GlobalConfiguration.Configuration);
		}

		public static void Configure(HttpConfiguration config)
		{
			config.Properties[typeof(ISessionFactory)] = SessionFactory;
			var routes = config.Routes;
			routes.MapHttpRoute(
				name: "DefaultApi",
				routeTemplate: "{controller}/{id}",
				defaults: new { id = RouteParameter.Optional });

			config.Filters.Add(new ExceptionFilter());
			config.Filters.Add(new SessionFilter());
			config.Filters.Add(new UserFilter());
		}
	}
}

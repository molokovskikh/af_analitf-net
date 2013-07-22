using System.Web.Http;
using AnalitF.Net.Service.Filters;
using Common.Web.Service.Filters;
using NHibernate;

namespace AnalitF.Net.Service.Config.Initializers
{
	public class Mvc
	{
		public void Run(HttpConfiguration config, NHibernate nhibernate, Config appConfig)
		{
			config.Properties[typeof(ISessionFactory)] = nhibernate.Factory;
			config.Properties["config"] = appConfig;

			var routes = config.Routes;
			routes.MapHttpRoute(
				name: "DefaultApi",
				routeTemplate: "{controller}/{id}",
				defaults: new { id = RouteParameter.Optional });
			config.Filters.Add(new ExceptionFilter());
			config.Filters.Add(new SessionFilter());
			config.Filters.Add(new UserFilter());
			config.Filters.Add(new ConfigFilter());
		}
	}
}
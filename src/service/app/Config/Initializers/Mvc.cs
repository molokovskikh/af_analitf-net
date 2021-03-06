﻿using System.Web.Http;
using AnalitF.Net.Service.Filters;
using Common.Web.Service.Filters;
using NHibernate;
using DebugFilter = AnalitF.Net.Service.Filters.DebugFilter;
using UserFilter = AnalitF.Net.Service.Filters.UserFilter;

namespace AnalitF.Net.Service.Config.Initializers
{
	public class Mvc
	{
		public void Run(HttpConfiguration config, ISessionFactory factory, Config appConfig)
		{
			config.Properties[typeof(ISessionFactory)] = factory;
			config.Properties["config"] = appConfig;

			var routes = config.Routes;
			routes.MapHttpRoute(
				name: "DefaultApi",
				routeTemplate: "{controller}/{id}",
				defaults: new { id = RouteParameter.Optional });
			config.Filters.Add(new ExceptionFilter());
			config.Filters.Add(new LogAccessErrors());
			config.Filters.Add(new DebugFilter());
			config.Filters.Add(new SessionFilter());
			config.Filters.Add(new UserFilter());
			config.Filters.Add(new ConfigFilter());
			config.Filters.Add(new LogAccess());
			config.Filters.Add(new Common.Web.Service.Filters.DebugFilter());
		}
	}
}
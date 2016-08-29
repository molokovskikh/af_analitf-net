using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Text;
using System.Web;
using System.Web.Http;
using AnalitF.Net.Service.Config.Environments;
using AnalitF.Net.Service.Config.Initializers;
using AnalitF.Net.Service.Controllers;
using Castle.Components.Binder;
using NHibernate;
using log4net;
using log4net.Config;
using NHibernate.Linq;

namespace AnalitF.Net.Service
{
	public class Application : HttpApplication
	{
		private ILog log = LogManager.GetLogger(typeof(Application));

		public static ISessionFactory SessionFactory;

		protected void Application_Start()
		{
			InitApp(GlobalConfiguration.Configuration);
		}

		protected void Application_Error()
		{
			log.Error("Ошибка в приложении", Server.GetLastError());
		}

		public static Config.Config InitApp(HttpConfiguration httpConfig, Service.Config.Config config = null)
		{
			XmlConfigurator.Configure();
			GlobalContext.Properties["Version"] = typeof(MainController).Assembly.GetName().Version;

			config = config ?? ReadConfig();
			if (config.Environment == "Development")
				new Development().Run(config);
			else
				new Production().Run(config);

			//в тестах мы можем запустить две копии приложения
			if (SessionFactory == null) {
				var nhibernate = new Config.Initializers.NHibernate();
				nhibernate.Init();
				SessionFactory = nhibernate.Factory;
			}
			ReadDbConfig(config);
			var mvc = new Mvc();
			mvc.Run(httpConfig, SessionFactory, config);
			new Config.Initializers.SmartOrderFactory().Init(SessionFactory);

			return config;
		}

		public static void ReadDbConfig(Config.Config config)
		{
			if (SessionFactory == null)
				return;
			using (var session = SessionFactory.OpenSession()) {
				var collection = new NameValueCollection();
				var rows = session.CreateSQLQuery("select `key`, `value` from Customers.AppConfig")
					.List<object[]>();
				foreach (var row in rows) {
					collection.Add(row[0].ToString(), row[1].ToString());
				}
				var builder = new TreeBuilder();
				var tree = builder.BuildSourceNode(collection);
				var binder = new DataBinder();
				binder.BindObjectInstance(config, "", tree);
			}
		}

		public static Config.Config ReadConfig()
		{
			var config = new Config.Config();
			var builder = new TreeBuilder();
			var tree = builder.BuildSourceNode(ConfigurationManager.AppSettings);
			var binder = new DataBinder();
			binder.BindObjectInstance(config, "", tree);
			return config;
		}
	}
}

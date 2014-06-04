using System.Configuration;
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

		public static Config.Config InitApp(HttpConfiguration httpConfig)
		{
			XmlConfigurator.Configure();
			GlobalContext.Properties["Version"] = typeof(MainController).Assembly.GetName().Version;

			var config = ReadConfig();
			if (config.Environment == "Development")
				new Development().Run(config);
			else
				new Production().Run(config);

			var nhibernate = new Config.Initializers.NHibernate();
			nhibernate.Init();
			SessionFactory = nhibernate.Factory;
			var mvc = new Mvc();
			mvc.Run(httpConfig, nhibernate, config);
			new Config.Initializers.SmartOrderFactory().Init(nhibernate);

			return config;
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

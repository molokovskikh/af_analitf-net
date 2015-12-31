using AnalitF.Net.Service.Config.Environments;
using Castle.ActiveRecord;
using NHibernate;
using NUnit.Framework;

namespace AnalitF.Net.Service.Test
{
	[SetUpFixture]
	public class FixtureSetup
	{
		public static ISessionFactory Factory;
		public static Config.Config Config;

		[OneTimeSetUp]
		public void Setup()
		{
			global::Test.Support.Setup.BuildConfiguration("local");
			var holder = ActiveRecordMediator.GetSessionFactoryHolder();

			var server = new Config.Initializers.NHibernate();
			server.Configuration = holder.GetConfiguration(typeof(ActiveRecordBase));
			server.Init();

			var factory = holder.GetSessionFactory(typeof(ActiveRecordBase));
			global::Test.Support.Setup.SessionFactory = factory;
			Factory = factory;
			Application.SessionFactory = factory;

			Config = Application.ReadConfig();
			new Development().Run(Config);
		}
	}
}
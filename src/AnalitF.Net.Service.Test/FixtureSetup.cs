using AnalitF.Net.Service.Config.Environments;
using NHibernate;
using NUnit.Framework;

namespace AnalitF.Net.Service.Test
{
	[SetUpFixture]
	public class FixtureSetup
	{
		public static ISessionFactory Factory;
		public static Config.Config Config;

		[SetUp]
		public void Setup()
		{
			global::Test.Support.Setup.Initialize();

			Config = Application.ReadConfig();
			new Development().Run(Config);

			var nhibernate = new Config.Initializers.NHibernate();
			nhibernate.Init();
			Factory = nhibernate.Factory;
			Application.SessionFactory = Factory;
		}
	}
}
using System;
using AnalitF.Net.Client;
using NHibernate;
using NHibernate.Cfg;
using NUnit.Framework;
using ReactiveUI;

namespace AnalitF.Net.Test.Integration
{
	[SetUpFixture]
	public class SetupFixture
	{
		public static ISessionFactory Factory;
		public static Configuration Configuration;

		[SetUp]
		public void Setup()
		{
			RxApp.LoggerFactory = t => new NullLogger();

			global::Test.Support.Setup.Initialize();
			AppBootstrapper.NHibernate = new Client.Config.Initializers.NHibernate();
			AppBootstrapper.NHibernate.Init();
			Factory = AppBootstrapper.NHibernate.Factory;
			Configuration = AppBootstrapper.NHibernate.Configuration;
		}
	}
}
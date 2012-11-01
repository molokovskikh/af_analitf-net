using NHibernate;
using NUnit.Framework;

namespace AnalitF.Net.Service.Test
{
	[SetUpFixture]
	public class FixtureSetup
	{
		public static ISessionFactory Factory;

		[SetUp]
		public void Setup()
		{
			global::Test.Support.Setup.Initialize();

			var nhibernate = new Config.Initializers.NHibernate();
			nhibernate.Init();
			Factory = nhibernate.Factory;
			Application.SessionFactory = Factory;
		}
	}
}
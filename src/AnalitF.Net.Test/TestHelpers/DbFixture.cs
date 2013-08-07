using AnalitF.Net.Test.Integration;
using NHibernate;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class DbFixture
	{
		protected ISession session;

		[SetUp]
		public void DbSetup()
		{
			session = SetupFixture.Factory.OpenSession();
		}

		[TearDown]
		public void DbTearDown()
		{
			if (session != null)
				session.Dispose();
		}
	}
}
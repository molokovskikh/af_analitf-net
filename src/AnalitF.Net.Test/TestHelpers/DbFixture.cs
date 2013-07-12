using AnalitF.Net.Test.Integration;
using NHibernate;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class DbFixture
	{
		protected ISession session;

		[SetUp]
		public void Setup()
		{
			session = SetupFixture.Factory.OpenSession();
		}

		[TearDown]
		public void TearDown()
		{
			if (session != null)
				session.Dispose();
		}
	}
}
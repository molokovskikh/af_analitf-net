using NUnit.Framework;

namespace AnalitF.Net.Test
{
	[SetUpFixture]
	public class SetupFixture
	{
		[SetUp]
		public void Setup()
		{
			global::Test.Support.Setup.Initialize();
		}
	}
}
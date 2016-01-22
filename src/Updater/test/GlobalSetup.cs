using System;
using NUnit.Framework;

namespace test
{
	[SetUpFixture]
	public class GlobalSetup
	{
		[OneTimeSetUp]
		public void Setup()
		{
			Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
		}
	}
}
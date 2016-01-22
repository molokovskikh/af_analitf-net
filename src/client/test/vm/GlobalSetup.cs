using System;
using NUnit.Framework;

namespace vm
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
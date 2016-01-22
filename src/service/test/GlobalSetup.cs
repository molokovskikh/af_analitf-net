using System;
using NUnit.Framework;

namespace AnalitF.Net.Service.Test
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
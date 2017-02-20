using System;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test
{
	[SetUpFixture]
	public class GlobalSetup
	{
		[OneTimeSetUp]
		public void Setup()
		{
			Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
			FileHelper.DeleteDirTimeout = TimeSpan.FromSeconds(5);
		}
	}
}
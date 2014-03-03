using System;
using System.Security.Policy;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit.Models
{
	[TestFixture]
	public class ConfigFixture
	{
		[Test]
		public void Get_wait_url()
		{
			var config = new Config.Config();
			config.BaseUrl = new Uri("http://localhost:1056/");
			var waybills = config.SyncUrl("waybills");
			Assert.AreEqual("http://localhost:1056/Main?reset=true&data=waybills", waybills.ToString());
			Assert.AreEqual("http://localhost:1056/Main", config.WaitUrl(waybills).ToString());
		}
	}
}
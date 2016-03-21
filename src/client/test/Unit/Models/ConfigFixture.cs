using System;
using System.Security.Policy;
using AnalitF.Net.Client.Models;
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
			var waybills = config.SyncUrl("waybills", null, null);
			Assert.AreEqual("http://localhost:1056/Main?reset=true&data=waybills", waybills.ToString());
			Assert.AreEqual("http://localhost:1056/Main?data=waybills", config.WaitUrl(waybills, "waybills").ToString());
			var url = config.SyncUrl(null, null, new [] { new Address { Id = 1 }, new Address { Id = 2 } });
			Assert.AreEqual("http://localhost:1056/Main?reset=true&data=&addressIds=1%2C2", url.ToString());
		}
	}
}
using System;
using AnalitF.Net.Service.Models;
using Common.Models;
using NUnit.Framework;

namespace AnalitF.Net.Service.Test
{
	[TestFixture, Ignore("Временно отключен")]
	public class ConfigFixture
	{
		[Test]
		public void Strip_branch()
		{
			var config = new Config.Config();
			config.UpdatePath = ".";
			var log = new RequestLog(new User(), new Version()) {
				Branch = "migration"
			};
			var data = new AnalitfNetData();
			var result = config.GetUpdatePath(data, log);
			Assert.AreEqual(".\\migration", result);

			data.BinUpdateChannel = "beta";
			result = config.GetUpdatePath(data, log);
			Assert.AreEqual(".\\migration-beta", result);

			data.BinUpdateChannel = "migration-beta";
			result = config.GetUpdatePath(data, log);
			Assert.AreEqual(".\\migration-beta", result);

			data.BinUpdateChannel = "migration";
			result = config.GetUpdatePath(data, log);
			Assert.AreEqual(".\\migration", result);

			data.BinUpdateChannel = "rtm";
			result = config.GetUpdatePath(data, log);
			Assert.AreEqual(".\\migration", result);

			log.Branch = "master";
			data.BinUpdateChannel = "migration";
			Assert.AreEqual(".\\rtm", config.GetUpdatePath(data, log));
		}
	}
}
using System.IO;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit.Models
{
	[TestFixture]
	public class ResultDirFixture
	{
		private Settings settings;
		private Config.Config config;

		[SetUp]
		public void Setup()
		{
			settings = new Settings();
			config = new Config.Config();
		}

		[Test]
		public void Cunfigure_autoopen()
		{
			var dir = new ResultDir("waybills", settings, config);
			Assert.IsFalse(dir.OpenFiles);

			settings.OpenWaybills = true;
			dir = new ResultDir("waybills", settings, config);
			Assert.True(dir.OpenFiles);
		}

		[Test]
		public void Open_waybill()
		{
			var dir = new ResultDir("waybills", settings, config);
			dir.ResultFiles.Add("waybill.txt");
			dir.ResultFiles.Add("waybill1.txt");
			var toOpen = ResultDir.OpenResultFiles(new [] { dir }).ToArray();
			Assert.AreEqual(1, toOpen.Length);
			Assert.AreEqual("Накладные", Path.GetFileName(((OpenResult)toOpen[0]).Filename));

			settings.OpenWaybills = true;
			dir = new ResultDir("waybills", settings, config);
			dir.ResultFiles.Add("waybill.txt");
			dir.ResultFiles.Add("waybill1.txt");
			toOpen = ResultDir.OpenResultFiles(new [] { dir }).ToArray();
			Assert.AreEqual(2, toOpen.Length);
			Assert.AreEqual("waybill.txt", Path.GetFileName(((OpenResult)toOpen[0]).Filename));
		}
	}
}
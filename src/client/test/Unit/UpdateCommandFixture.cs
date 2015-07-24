using System.IO;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit
{
	[TestFixture]
	public class UpdateCommandFixture
	{
		private FileCleaner cleaner;

		[SetUp]
		public void Setup()
		{
			cleaner = new FileCleaner();
		}

		[TearDown]
		public void TearDown()
		{
			cleaner.Dispose();
		}

		[Test]
		public void Calculate_message()
		{
			var command = new UpdateCommand();
			command.SyncData = "Waybills";
			Assert.AreEqual("Получение документов завершено успешно.", command.SuccessMessage);
		}

		[Test]
		public void Clear_ads()
		{
			var cfg = new Config.Config {
				RootDir = "test-tmp"
			};
			cleaner.WatchDir(cfg.RootDir);
			var adUpdateDir = Path.Combine(cfg.UpdateTmpDir, "ads");
			FileHelper.CreateDirectoryRecursive(adUpdateDir);
			File.WriteAllText(Path.Combine(adUpdateDir, "delete.me"), "");

			var ads = cfg.KnownDirs(new Settings(true)).First(d => d.Name == "ads");
			FileHelper.CreateDirectoryRecursive(ads.Dst);
			File.WriteAllText(Path.Combine(ads.Dst, "2block.gif"), "");

			new UpdateCommand().Move(ads);
			Assert.AreEqual("test-tmp\\ads\\delete.me", Directory.GetFiles(ads.Dst).Implode());
		}
	}
}
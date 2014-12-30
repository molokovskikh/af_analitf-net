using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Common.Tools;
using NUnit.Framework;
using Updater;

namespace test
{
	[TestFixture]
	public class UpdateFixture
	{
		[Test, RequiresSTA]
		public void Run_after_update_on_exited_process()
		{
			FileHelper.InitDir("test", "test/bin", "test/update");
			File.Copy("../../../stub/bin/debug/stub.exe", "test/update/stub.exe");
			File.WriteAllText("test/update/version.txt", "1.0");

			var bin = Path.GetFullPath("test/bin/stub.exe");
			var updateRoot = Path.GetFullPath("test/update");
			//симулируем запуск если обновляемое приложение уже завергилось
			var w = new MainWindow(true);
			var process = w.Run(-1, bin, updateRoot).Result;

			process.WaitForExit();
			Assert.IsTrue(File.Exists("test/bin/started"));
		}

		[Test]
		public void Clean_bin_on_marker_file()
		{
			FileHelper.InitDir("test", "test/bin", "test/update");
			File.WriteAllText("test/bin/app.exe", "");
			File.WriteAllText("test/bin/lib.dll", "");

			File.WriteAllText("test/update/app.exe", "");
			File.WriteAllText("test/update/delete.me", "*.dll\r\n*.exe");
			File.WriteAllText("test/update/version.txt", "1.0");

			MainWindow.Update(-1, "test/bin/app.exe", "test/update");
			var files = Directory.GetFiles("test/bin").Select(f => Path.GetFileName(f));
			Assert.AreEqual("app.exe", files.Implode());
		}
	}
}

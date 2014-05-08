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
			File.Copy("../../../test.stub/bin/debug/test.stub.exe", "test/update/test.stub.exe");
			File.WriteAllText("test/update/version.txt", "1.0");

			var bin = Path.GetFullPath("test/bin/test.stub.exe");
			var updateRoot = Path.GetFullPath("test/update");
			//симулируем запуск если обновляемое приложение уже завергилось
			var w = new MainWindow(true);
			var process = w.Run(-1, bin, updateRoot).Result;

			process.WaitForExit();
			Assert.IsTrue(File.Exists("test/bin/started"));
		}
	}
}

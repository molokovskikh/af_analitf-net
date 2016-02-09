using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BsDiff;
using Common.Tools;
using NUnit.Framework;
using Updater;

namespace test
{
	[TestFixture]
	public class UpdateFixture
	{
		[Test, Apartment(ApartmentState.STA)]
		public void Run_after_update_on_exited_process()
		{
			FileHelper.InitDir("test/bin", "test/update");
			File.Copy("../../../stub/bin/debug/stub.exe", "test/update/stub.exe");
			File.WriteAllText("test/update/version.txt", "1.0");

			var bin = Path.GetFullPath("test/bin/stub.exe");
			var updateRoot = Path.GetFullPath("test/update");
			//симулируем запуск если обновляемое приложение уже завершилось
			var w = new MainWindow(true);
			var process = w.Run(-1, bin, updateRoot).Result;

			process.WaitForExit();
			Assert.IsTrue(File.Exists("test/bin/started"));
		}

		[Test]
		public void Clean_bin_on_marker_file()
		{
			FileHelper.InitDir("test/bin", "test/update");
			FileHelper.Touch("test/bin/app.exe");
			FileHelper.Touch("test/bin/lib.dll");

			FileHelper.Touch("test/update/app.exe");
			File.WriteAllText("test/update/delete.me", "*.dll\r\n*.exe");
			File.WriteAllText("test/update/version.txt", "1.0");

			MainWindow.Update(-1, "test/bin/app.exe", "test/update");
			var files = Directory.GetFiles("test/bin").Select(f => Path.GetFileName(f));
			Assert.AreEqual("app.exe", files.Implode());
		}

		[Test]
		public void Apply_bs_diff()
		{
			FileHelper.InitDir("test/bin", "test/update");
			File.WriteAllText("test/bin/app.exe", "123");

			using (var diff = File.Create("test/update/app.exe.bsdiff"))
				BinaryPatchUtility.Create(Encoding.UTF8.GetBytes("123"), Encoding.UTF8.GetBytes("123456"), diff);
			File.WriteAllText("test/update/version.txt", "1.0");

			MainWindow.Update(-1, "test/bin/app.exe", "test/update");
			var files = Directory.GetFiles("test/bin").Select(f => Path.GetFileName(f));
			Assert.AreEqual("app.exe", files.Implode());
			Assert.AreEqual("123456", File.ReadAllText("test/bin/app.exe"));
		}
	}
}

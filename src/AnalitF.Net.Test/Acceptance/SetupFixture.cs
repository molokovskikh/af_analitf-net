using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Automation;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Acceptance
{
	[TestFixture, Explicit]
	public class SetupFixture : BaseFixture
	{
		//string source = @"\\VBOXSVR\setup\setup.exe";
		string source = @"C:\Projects\Production\AnalitF.Net\output\setup\setup.exe";

		[Test]
		public void Install()
		{
			//VBoxManage.exe list runningvms
			//VBoxManage.exe startvm "Windows 7"
			//VBoxManage.exe guestcontrol "Windows 7" exec --image "C:\Windows\notepad.exe" --username "test" --wait-stdout --verbose
			var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "setup.exe");
			File.Copy(source, file, true);
			Process = StartProcess(file);
			WaitMainWindow();

			WaitWindow("АналитФАРМАЦИЯ");
			Click("Установить");

			//WaitWindow("Установка Microsoft .NET Framework 4");
			//WaitText("Установка завершена");
			//Click("Готово");

			//Window("АналитФАРМАЦИЯ");
			WaitText("Установка завершена успешно");
			Click("Закрыть");

			var exe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "АналитФАРМАЦИЯ", "AnalitF.Net.Client.exe");
			Assert.IsTrue(File.Exists(exe));
		}
	}
}
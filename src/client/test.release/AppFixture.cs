using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Windows.Automation;
using AnalitF.Net.Client.Test.Acceptance;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Tools.Helpers;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using NUnit.Framework;
using TestStack.White.InputDevices;

namespace test.release
{
	[TestFixture, Explicit("Тест должен запускаться после подготовки релиза")]
	public class AppFixture : BaseFixture
	{
		//для отладки
		//string testUserName = Environment.UserName;
		//string testPassword = "123";
		//string prevSetupBin = @"..\\..\\..\\..\\..\\output\\setup\\setup.exe";

		string setupBin = @"..\\..\\..\\..\\..\\output\\setup\\setup.exe";
		string archiveDir = @"\\offdc\MMedia\AnalitF.Net";
		string name = "АналитФАРМАЦИЯ";
		string prevSetupBin = null;
		string testUserName = "26307";
		string testPassword = "TkGJEQUX";
		string setupId = "{22DC7E87-F9E2-463F-9811-E0C53779C644}";

		[SetUp]
		public void Setup()
		{
			IsDebug = false;
			Timeout = 20.Second();
			UpdateTimeout = 5.Minute();
			var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "АналитФАРМАЦИЯ");
			Bin = Path.Combine(root, "AnalitF.Net.Client.exe");
			Close(Process.GetProcesses().FirstOrDefault(p => {
				try {
					return p.MainModule.FileName == Path.GetFullPath(Bin);
				}
				catch(Exception) {
					return false;
				}
			}));
			if (Directory.Exists(Path.Combine(root, "data")))
				Directory.Delete(Path.Combine(root, "data"), true);
		}

		[Test]
		public void Update_db()
		{
			Uninstall();
			Install(setupBin);

			Activate();
			WaitMessage("Для начала работы с программой необходимо заполнить учетные данные");

			Type("Settings_UserName", testUserName);
			Type("Password", testPassword);

			var dialog = WaitDialog("Настройка");
			Click("Save", dialog);

			WaitIdle();
			Click("Update", MainWindow);
			AssertUpdate("Обновление завершено успешно.");
		}

		[Test]
		public void Version_update()
		{
			Uninstall();

			var currentVersion = new Version(FileVersionInfo.GetVersionInfo(setupBin).ProductVersion);
			var prev = GetPrevVersion(currentVersion);
			var prevVersion = new Version(FileVersionInfo.GetVersionInfo(prev).ProductVersion);
			Install(prev);

			Activate();
			WaitMessage("Для начала работы с программой необходимо заполнить учетные данные");
			Assert.That(AutomationHelper.ToText(MainWindow), Does.Contain(prevVersion.ToString()));

			Type("Settings_UserName", testUserName);
			Type("Password", testPassword);

			var dialog = WaitDialog("Настройка");
			Click("Save", dialog);

			WaitIdle();
			Click("Update", MainWindow);
			AssertUpdate("Получена новая версия программы. Сейчас будет выполнено обновление.");

			FilterByProcess = false;
			var update = Opened.Timeout(Timeout).First();
			AssertText(update, "Внимание! Происходит обновление программы.");

			update = Opened.Where(e => e.GetName() == "Обмен данными").Timeout(15.Second()).First();
			AssertText(update, "Производится обмен данными");
			Process = Process.GetProcessById(update.GetProcessId());
			FilterByProcess = true;
			MainWindow = AutomationElement.RootElement.FindFirst(TreeScope.Descendants, new AndCondition(
				new PropertyCondition(AutomationElement.ProcessIdProperty, Process.Id),
				new PropertyCondition(AutomationElement.NameProperty, "АналитФАРМАЦИЯ")));

			var message = Opened.Timeout(UpdateTimeout).First();
			AssertText(message, "Обновление завершено успешно.");
			Assert.That(AutomationHelper.ToText(MainWindow), Does.Contain(currentVersion.ToString()));
			ClickByName("ОК", message);
		}

		private string GetPrevVersion(Version version)
		{
			if (!String.IsNullOrEmpty(prevSetupBin))
				return prevSetupBin;
			var prevName = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(setupBin)), "prev-setup.exe");
			File.Delete(prevName);
			var pattern = new Regex(@"analitf\.net.(\d+\.\d+\.\d+\.\d+)\.exe", RegexOptions.IgnoreCase);
			var setup = Directory.GetFiles(archiveDir)
				.Where(f => pattern.IsMatch(Path.GetFileName(f)))
				.Select(f => Tuple.Create(f, Version.Parse(pattern.Match(Path.GetFileName(f)).Groups[1].Value)))
				.Where(t => t.Item2 < version)
				.OrderByDescending(f => f.Item2)
				.Select(t => t.Item1)
				.First();
			File.Copy(setup, prevName);
			return prevName;
		}

		private void Install(string bin)
		{
			ProcessHelper.Cmd("{0} {1}", bin, "/quiet");
		}

		private void Uninstall()
		{
			var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\");
			if (key == null)
				return;
			var cmd = "";
			foreach (var subKeyName in key.GetSubKeyNames()) {
				var subkey = key.OpenSubKey(subKeyName);
				var id = ((string[])subkey?.GetValue("BundleUpgradeCode"))?.FirstOrDefault();
				if (setupId.Match(id)) {
					cmd = subkey.GetValue("QuietUninstallString") as string;
					break;
				}
			}
			if (String.IsNullOrEmpty(cmd))
				return;
			ProcessHelper.Cmd(cmd);
		}
	}
}

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Tools.Helpers;
using NUnit.Framework;
using VirtualBox;

namespace vm
{
	[TestFixture, Explicit, Category("VM")]
	public class VMFixture
	{
		string user = "test";
		string password = "123";
		string root;
		string setup;
		bool forceShutdown = true;
		bool killLeakedLocks = true;

		VirtualBoxClass vm;
		SessionClass session;
		IMachine machine;

		//string guiType = "gui";
		string guiType = "headless";
		string vmName = "winxp";

		[SetUp]
		public void Setup()
		{
			root = Path.GetFullPath(@"..\..\..\..\..\..\");
			setup = Path.Combine(root, @"output\setup\setup.exe");

			//кэшируем файл что бы каждый раз не собирать
			if (!File.Exists("setup.exe")) {
				var cmd = $"C:\\cygwin\\bin\\bash.exe -l -c \"cd `cygpath '{root}'`;bake build:client env=test\"";
				ProcessHelper.CmdDir(cmd, root, timeout: 5.Minute());
				File.Copy(setup, "setup.exe");
			}

			Init();
		}

		[TearDown]
		public void TearDown()
		{
			Shutdown();
		}

		[Test]
		public void Install()
		{
			var guestsession = Boot();

			var guestRoot = $@"C:\users\{user}";
			var installRoot = Path.Combine(guestRoot, @"appdata\local\аналитфармация\");
			if (machine.OSTypeId == "WindowsXP") {
				guestRoot = $@"C:\Documents and Settings\{user}";
				installRoot = Path.Combine(guestRoot, @"Local Settings\Application Data\АналитФАРМАЦИЯ");
			}

			var target = Path.Combine(guestRoot, "setup.exe");
			var copyTo = guestsession.CopyTo(setup, target, null);
			copyTo.WaitForCompletion(-1);
			Assert.IsNull(copyTo.ErrorInfo, copyTo.ErrorInfo?.Text);

			Start(guestsession, target, "/quiet");
			var exe = Path.Combine(installRoot, "AnalitF.Net.Client.exe");
			Start(guestsession, exe, "--quiet", "start-check");

			var log = Path.Combine(installRoot, "AnalitF.Net.Client.log");
			File.Delete("log.txt");
			var copyLog = guestsession.CopyFrom(log, Path.GetFullPath("log.txt"), null);
			copyLog.WaitForCompletion(-1);
			Assert.IsNull(copyLog.ErrorInfo, copyLog.ErrorInfo?.Text);
			var logText = File.ReadAllText("log.txt");
			Assert.That(logText, Is.Not.StringContaining("ERROR"));
		}

		[Test, Ignore]
		public void Snap()
		{
			machine.LockMachine(session, LockType.LockType_Write);

			if (machine.SnapshotCount > 0) {
				var snapshot = machine.FindSnapshot("origin");
				session.Console.DeleteSnapshot(snapshot.Id).WaitForCompletion(-1);
			}

			session.Console.TakeSnapshot("origin", "чистое состояние").WaitForCompletion(-1);
			session.UnlockMachine();
		}

		private static void Start(IGuestSession guestsession, string target, params string[] arguments)
		{
			var setupProcess = guestsession.ProcessCreateEx(target, arguments,
				null, null,
				0, ProcessPriority.ProcessPriority_Default, null);
			var cmd = new[] { target }.Concat(arguments).Implode(" ");
			Assert.AreEqual(setupProcess.Status, ProcessStatus.ProcessStatus_Starting, cmd);
			while (setupProcess.Status == ProcessStatus.ProcessStatus_Started
				|| setupProcess.Status == ProcessStatus.ProcessStatus_Starting) {
				Thread.Sleep(1000);
			}
			Assert.AreEqual(ProcessStatus.ProcessStatus_TerminatedNormally, setupProcess.Status, cmd);
		}

		private IGuestSession Boot()
		{
			if (forceShutdown) {
				Shutdown();
				Restore();
			}

			if (machine.State != MachineState.MachineState_Running) {
				Start();
			}
			else {
				machine.LockMachine(session, LockType.LockType_Shared);
			}

			return WaitBoot();
		}

		private IGuestSession WaitBoot()
		{
			var console = session.Console;
			var guestsession = StartGuest(console);

			var done = false;
			var i = 0;
			while (!done) {
				try {
					i++;
					if (i > 300)
						throw new Exception("Не удалось дождать запуска виртуальной машины за 5 минут," +
							$" проверь имя пользователя и пароль для входа, пытаюсь войти как {user} {password}");
					Thread.Sleep(1000);
					var echoTest = guestsession.ProcessCreate("C:\\windows\\system32\\cmd.exe", new[] { "/c", "echo", "hello!" }, null, null, 0);
					WaitForExit(echoTest);
					done = echoTest.Status == ProcessStatus.ProcessStatus_TerminatedNormally;
				}
				catch (COMException e) {
					Assert.That(e.Message, Is.StringContaining("The guest execution service is not ready (yet)"));
				}
			}
			return guestsession;
		}

		private static void WaitForExit(IGuestProcess echoTest)
		{
			while (echoTest.Status != ProcessStatus.ProcessStatus_TerminatedNormally
				&& echoTest.Status != ProcessStatus.ProcessStatus_TerminatedAbnormally
				&& echoTest.Status != ProcessStatus.ProcessStatus_Error) {
				Thread.Sleep(1000);
			}
		}

		private void Init()
		{
			vm = new VirtualBoxClass();
			session = new SessionClass();
			machine = vm.Machines.Cast<IMachine>().First(i => i.Name == vmName);
		}

		private void Start()
		{
			try
			{
				var process = machine.LaunchVMProcess(session, guiType, "");
				process.WaitForCompletion(-1);
			}
			catch(COMException e) {
				if (e.Message.Contains("is already locked by a session")) {
					Process.GetProcessesByName("VBoxSVC").Each(p => p.Kill());
					Process.GetProcessesByName("VirtualBox").Each(p => p.Kill());
					Thread.Sleep(3000);
					Init();
					var process = machine.LaunchVMProcess(session, guiType, "");
					process.WaitForCompletion(-1);
				}
			}
		}

		private IGuestSession StartGuest(IConsole console)
		{
			var guestsession = console.Guest.CreateSession(user, password, "", "test");
			WaitHelper.WaitOrFail(10.Second(),
				() => guestsession.Status == GuestSessionStatus.GuestSessionStatus_Started,
				$"состояние сессии {guestsession.Status} ждем GuestSessionStatus.GuestSessionStatus_Started");
			return guestsession;
		}

		private void Shutdown()
		{
			if (machine == null || machine.State != MachineState.MachineState_Running)
				return;

			//если блокировки нет будет исключение
			try {
				session.UnlockMachine();
			}
			catch (Exception) {}

			machine.LockMachine(session, LockType.LockType_Shared);
			try {
				session.Console.PowerButton();
				//var guestsession = StartGuest(session.Console);
				//var shutdown = guestsession.ProcessCreate("cmd.exe", new[] {"/C", "shutdown", "/p"}, null, null, 0);
				//Assert.That(shutdown.Status, Is.EqualTo(ProcessStatus.ProcessStatus_Starting)
				//	.Or.EqualTo(ProcessStatus.ProcessStatus_Started));
			}
			finally {
				session.UnlockMachine();
			}

			WaitState(machine, MachineState.MachineState_PoweredOff);
		}

		private static void WaitState(IMachine machine, MachineState state)
		{
			WaitHelper.WaitOrFail(30.Second(),
				() => machine.State == state,
				$"состояния машины {state} текущее состояние {machine.State}");
		}

		private void Restore()
		{
			machine.LockMachine(session, LockType.LockType_Write);
			try {
				if (machine.SnapshotCount == 0) {
					session.Console.TakeSnapshot("origin", "чистое состояние").WaitForCompletion(-1);
				}
				else {
					var snapshot = machine.FindSnapshot("origin");
					session.Console.RestoreSnapshot(snapshot).WaitForCompletion(-1);
				}
			}
			finally {
				session.UnlockMachine();
			}
		}
	}
}
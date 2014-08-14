using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Common.Tools;
using Common.Tools.Helpers;
using NUnit.Framework;
using VirtualBox;

namespace vm
{
	[TestFixture, Explicit, Category("VM")]
	public class VMFixture
	{
		string user = "IEUser";
		string password = "12345678";
		string root;
		string setup;
		bool forceShutdown = true;
		bool killLeakedLocks = true;

		VirtualBoxClass vm;
		SessionClass session;
		IMachine machine;

		//string guiType = "gui";
		string guiType = "headless";

		public VMFixture()
		{
			root = Path.GetFullPath(@"..\..\..\..\..\..\");
			setup = Path.Combine(root, @"output\setup\setup.exe");
		}

		[SetUp]
		public void Setup()
		{
			//кэшируем файл что бы каждый раз не собирать
			if (!File.Exists("setup.exe")) {
				var cmd = String.Format("bash.exe -l -c \"cd `cygpath '{0}'`;{1}\"", root, "bake build:client env=test");
				ProcessHelper.CmdDir(cmd, root);
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
		public void Install_win7()
		{
			var guestsession = Boot();

			var target = String.Format(@"C:\users\{0}\setup.exe", user);
			var copyTo = guestsession.CopyTo(setup, target, null);
			copyTo.WaitForCompletion(-1);
			Assert.IsNull(copyTo.ErrorInfo, copyTo.ErrorInfo != null ? copyTo.ErrorInfo.Text : "");

			Start(guestsession, target, "/quiet");
			var exe = String.Format(@"C:\users\{0}\appdata\local\аналитфармация\AnalitF.Net.Client.exe", user);
			Start(guestsession, exe, "--quiet", "start-check");

			var log = String.Format(@"C:\users\{0}\appdata\local\аналитфармация\AnalitF.Net.Client.log", user);
			File.Delete("log.txt");
			var copyLog = guestsession.CopyFrom(log, Path.GetFullPath("log.txt"), null);
			copyLog.WaitForCompletion(-1);
			Assert.IsNull(copyLog.ErrorInfo, copyLog.ErrorInfo != null ? copyLog.ErrorInfo.Text : "");
			var logText = File.ReadAllText("log.txt");
			Assert.AreEqual("", logText);
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
			Assert.AreEqual(setupProcess.Status, ProcessStatus.ProcessStatus_TerminatedNormally, cmd);
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
			WaitState(machine, MachineState.MachineState_Running);
			var guestsession = StartGuest(console);

			var done = false;
			while (!done) {
				try {
					Thread.Sleep(1000);
					var echoTest = guestsession.ProcessCreate("cmd.exe", new[] { "/c", "echo" }, null, null, 0);
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
			machine = vm.Machines.Cast<IMachine>().First(i => i.Name == "win7");
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
			while (guestsession.Status != GuestSessionStatus.GuestSessionStatus_Started) {
				Thread.Sleep(1000);
			}
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
			var guestsession = StartGuest(session.Console);
			var shutdown = guestsession.ProcessCreate("cmd.exe", new[] {"/C", "shutdown", "/p"}, null, null, 0);
			Assert.That(shutdown.Status, Is.EqualTo(ProcessStatus.ProcessStatus_Starting)
				.Or.EqualTo(ProcessStatus.ProcessStatus_Started));
			session.UnlockMachine();

			WaitState(machine, MachineState.MachineState_PoweredOff);
		}

		private static void WaitState(IMachine machine, MachineState state)
		{
			while (machine.State != state) {
				Thread.Sleep(1000);
			}
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

		private void Restore()
		{
			machine.LockMachine(session, LockType.LockType_Write);
			var snapshot = machine.FindSnapshot("origin");
			session.Console.RestoreSnapshot(snapshot).WaitForCompletion(-1);
			session.UnlockMachine();
		}
	}
}
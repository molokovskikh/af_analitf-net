using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading;
using System.Windows;
using AnalitF.Net.Client.Helpers;
using Common.Tools.Calendar;
using NPOI.HSSF.Record;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Models
{
	public class SingleInstance : IDisposable
	{
		private Mutex mutex;
		private Mutex shutdown;
		private Mutex startup;
		private string name;
		private string shutdownName;
		private string startupName;
		private bool isStarted = true;

		public ManualResetEventSlim WaitShutdown = new ManualResetEventSlim();
		public ManualResetEventSlim WaitStartup = new ManualResetEventSlim();
		public TimeSpan Timeout = 30.Second();

		public SingleInstance(string name)
		{
			this.name = "Local\\" + name + "-" + typeof(SingleInstance).Assembly.Location.GetHashCode();
			shutdownName = name + "-shutdown";
			startupName = name + "-startup";
		}

		public bool TryStartAndWait()
		{
			if (TryStart()) {
				Wait();
				return true;
			}
			return false;
		}

		public bool TryStart()
		{
			mutex = new Mutex(true, name, out isStarted);
			if (isStarted) {
				startup = new Mutex(true, startupName);
				return true;
			}

			//если мы не смогли наложить блокировку на завершение работы
			//значит другой процесс завершается и нам нужно подождать его
			//выходим что бы показать заставку и вызвать Wait
			if (IsShutdownInProcess()) {
				return true;
			}

			bool locked = false;
			try {
				startup = new Mutex(true, startupName, out locked);
				if (!locked) {
					WaitStartup.Set();
					try {
						locked = startup.WaitOne(Timeout);
					} catch (AbandonedMutexException) {
						locked = true;
					}
					if (!locked)
						Fail();
				}
			}
			finally {
				if (locked)
					startup.ReleaseMutex();
				startup.Dispose();
				startup = null;
			}
			var success = TryActivateProcessWindow();
			if (!success)
				Fail();
			return false;
		}

		private bool IsShutdownInProcess()
		{
			bool isShutdownLocked;
			using (var checkMutex = new Mutex(true, shutdownName, out isShutdownLocked)) {
				if (isShutdownLocked)
					checkMutex.ReleaseMutex();
			}
			return !isShutdownLocked;
		}

		private static void Fail()
		{
			throw new EndUserError(
				"АналитФАРМАЦИЯ уже запущена, но не отвечает. Чтобы запустить АналитФАРМАЦИЯ," +
					" вы должны сперва завершить запущенный процесс или перезагрузить компьютер.");
		}

		private static IntPtr GetCurrentInstanceWindowHandle()
		{
			var process = FindProcess();
			if (process == null)
				return IntPtr.Zero;
			return process.MainWindowHandle;
		}

		private static Process FindProcess()
		{
			var currentProcess = Process.GetCurrentProcess();
			var process = Process.GetProcessesByName(currentProcess.ProcessName)
				.FirstOrDefault(p => p.Id != currentProcess.Id
					&& currentProcess.MainModule.FileName == p.MainModule.FileName);
			return process;
		}

		protected virtual bool TryActivateProcessWindow()
		{
			var handle = GetCurrentInstanceWindowHandle();
			if (handle == IntPtr.Zero)
				return false;

			if (WinApi.IsIconic(handle))
				WinApi.ShowWindow(handle, 9);

			return WinApi.SetForegroundWindow(handle);
		}

		public void Dispose()
		{
			if (mutex != null) {
				if (isStarted)
					mutex.ReleaseMutex();
				mutex.Dispose();
				mutex = null;
			}

			if (shutdown != null) {
				shutdown.ReleaseMutex();
				shutdown.Dispose();
				shutdown = null;
			}

			if (startup != null) {
				startup.ReleaseMutex();
				startup.Dispose();
				startup = null;
			}
		}

		public void SignalShutdown()
		{
			if (isStarted)
				shutdown = new Mutex(true, shutdownName);
		}

		public void Wait()
		{
			if (isStarted)
				return;

			WaitShutdown.Set();
			try {
				isStarted = mutex.WaitOne(Timeout);
			} catch(AbandonedMutexException e) {
				isStarted = true;
			}
			if (!isStarted)
				Fail();
		}

		public void SignalStartup()
		{
			if (startup != null) {
				startup.ReleaseMutex();
				startup.Dispose();
				startup = null;
			}
		}
	}
}
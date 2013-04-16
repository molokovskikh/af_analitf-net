using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Models
{
	public class SingleInstance : IDisposable
	{
		private Mutex mutex;
		private string name;

		public SingleInstance(string name)
		{
			this.name = name;
		}

		[DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll")]
		private static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern bool IsIconic(IntPtr hWnd);

		public bool IsApplicationAlreadyRunning()
		{
			var applicationName = name;
			bool isNew;

			mutex = new Mutex(true, "Local\\" + applicationName, out isNew);
			if (isNew)
				mutex.ReleaseMutex();

			return !isNew;
		}

		public bool Check()
		{
			if (IsApplicationAlreadyRunning()) {
				var success = SwitchToCurrentInstance();
				if (!success) {
					throw new EndUserError("АналитФАРМАЦИЯ уже запущена, но не отвечает. Чтобы запустить АналитФАРМАЦИЯ, вы должны сперва завершить запущенный процесс или перезагрузить компьютер.");
				}
				return false;
			}
			return true;
		}

		private static IntPtr GetCurrentInstanceWindowHandle()
		{
			var currentProcess = Process.GetCurrentProcess();
			var processes = Process.GetProcessesByName(currentProcess.ProcessName);
			foreach (var process in processes) {
				if (currentProcess.Id != process.Id &&
					Path.GetFileName(currentProcess.MainModule.FileName) == Path.GetFileName(process.MainModule.FileName))
					return process.MainWindowHandle;
			}
			return IntPtr.Zero;
		}

		private static bool SwitchToCurrentInstance()
		{
			var handle = GetCurrentInstanceWindowHandle();
			if (handle == IntPtr.Zero)
				return false;

			if (IsIconic(handle))
				ShowWindow(handle, 9);

			return SetForegroundWindow(handle);
		}

		public void Dispose()
		{
			if (mutex != null)
				mutex.Dispose();
		}
	}
}
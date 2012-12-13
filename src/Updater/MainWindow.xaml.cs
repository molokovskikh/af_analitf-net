using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Common.Tools;
using log4net;
using Path = System.IO.Path;

namespace Updater
{
	public partial class MainWindow
	{
		private ILog log = LogManager.GetLogger(typeof(MainWindow));

		public MainWindow()
		{
			InitializeComponent();

			int pid;
			var args = Environment.GetCommandLineArgs();
			if (args.Length < 2 || !int.TryParse(args[1], out pid)) {
				pid = -1;
			}

			string mainModule = null;
			var process = GetProcess(pid);
			if (process != null) {
				mainModule = process.MainModule.FileName;
				log.DebugFormat("По завершении будет запущен процесс {0}", mainModule);
			}

			if (process != null && process.MainWindowHandle != IntPtr.Zero) {
				new WindowInteropHelper(this) {
					Owner = process.MainWindowHandle
				};
				WindowStartupLocation = WindowStartupLocation.CenterOwner;
			}
			else {
				WindowStartupLocation = WindowStartupLocation.CenterScreen;
			}

			ResizeMode = ResizeMode.NoResize;
			SizeToContent = SizeToContent.Manual;
			var task = new Task(() => Update(pid, mainModule));
			task.ContinueWith(t => {
				if (t.IsFaulted) {
					App.NotifyAboutException(t.Exception);
				}
				Close();
			}, TaskScheduler.FromCurrentSynchronizationContext());
			task.Start();
		}

		public void Update(int pid, string parentExecutable = null)
		{
			WaitForPid(pid);

			var sourceRootPath = FileHelper.MakeRooted(".");
			var destRootPath = Path.Combine(sourceRootPath, "..\\..");

			var selfExe = Path.GetFileNameWithoutExtension(typeof(MainWindow).Assembly.Location);
			var version = File.ReadAllText(Path.Combine(sourceRootPath, "version.txt")).Trim();
			var newBinPath = Path.Combine(destRootPath, version);
			var oldBinPath = Path.Combine(destRootPath, "bin");
			var exePath = destRootPath;
			var files = Directory.GetFiles(sourceRootPath);

			files = files.Where(f => !Path.GetFileNameWithoutExtension(f).Match(selfExe)).ToArray();
			var exeFiles = files.Where(f => Path.GetExtension(f).Match(".exe")).ToArray();
			exeFiles = exeFiles.Concat(files.Where(f => Path.GetExtension(f).Match(".config")
				&& exeFiles.Any(e => Path.GetFileNameWithoutExtension(e).Match(Path.GetFileNameWithoutExtension(f)))))
				.ToArray();
			var binFiles = files.Except(exeFiles).ToArray();

			if (Directory.Exists(oldBinPath)) {
				if (Directory.Exists(newBinPath))
					Directory.Delete(newBinPath, true);
				Directory.CreateDirectory(newBinPath);
			}
			else {
				newBinPath = exePath;
			}

			CopyFiles(binFiles, newBinPath);
			if (Directory.Exists(oldBinPath)) {
				Directory.Delete(oldBinPath, true);
				Directory.Move(newBinPath, oldBinPath);
			}
			CopyFiles(exeFiles, exePath);

			if (!String.IsNullOrEmpty(parentExecutable)) {
				var arguments = "import";
				log.DebugFormat("запускаю процесс {0} {1}", parentExecutable, arguments);
				Process.Start(parentExecutable, arguments);
			}
		}

		private void WaitForPid(int pid)
		{
			var process = GetProcess(pid);
			if (process == null)
				return;

			var waitTimeout = 60 * 1000;
			log.DebugFormat("Жду завершения процесса {0}", process.Id);
			var exited = process.WaitForExit(waitTimeout);
			if (!exited) {
				try {
					log.DebugFormat("Процесс {0} не завершился за 1 минуту, завершаю принудительно", pid);
					process.Kill();
				}
				//если пока думали все завершилось
				catch (InvalidOperationException) {}
			}
		}

		private static Process GetProcess(int pid)
		{
			if (pid < 1)
				return null;

			try {
				return Process.GetProcessById(pid);
			}
			//если процесс уже завершилса
			catch (ArgumentException) {}
			return null;
		}

		private void CopyFiles(IEnumerable<string> sources, string path)
		{
			foreach (var source in sources) {
				var dest = Path.Combine(path, Path.GetFileName(source));
				log.DebugFormat("Копирую {0} > {1}", source, dest);
				File.Copy(source, dest, true);
			}
		}
	}
}

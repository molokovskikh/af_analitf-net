using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Common.Tools;
using log4net;
using Path = System.IO.Path;

namespace Updater
{
	public class FileNameComparer : IEqualityComparer<string>
	{
		public bool Equals(string x, string y)
		{
			return StringComparer.CurrentCultureIgnoreCase.Equals(Path.GetFileName(x), Path.GetFileName(y));
		}

		public int GetHashCode(string obj)
		{
			return StringComparer.CurrentCultureIgnoreCase.GetHashCode(Path.GetFileName(obj));
		}
	}

	public partial class MainWindow
	{
		private ILog log = LogManager.GetLogger(typeof(MainWindow));

		public MainWindow(bool stub)
		{
		}

		public MainWindow()
		{
			log.DebugFormat("Обновление запущено {0}", typeof(MainWindow).Assembly.Location);
			var args = Environment.GetCommandLineArgs();
			var pid = args.Skip(1).Select(v => SafeConvert.ToInt32(v, -1)).FirstOrDefault(-1);
			var exe = args.Skip(2).FirstOrDefault();

			Closed += (sender, eventArgs) => {
				log.DebugFormat("Обновление завершено");
			};
			var task = Run(pid, exe, FileHelper.MakeRooted("."));
			task.ContinueWith(t => {
				if (t.IsFaulted) {
					log.Error("Процесс обновления завершился ошибкой", t.Exception);
					App.NotifyAboutException(t.Exception);
				}
				Close();
			}, TaskScheduler.FromCurrentSynchronizationContext());
			InitializeComponent();
			ResizeMode = ResizeMode.NoResize;
			SizeToContent = SizeToContent.Manual;
		}

		public Task<Process> Run(int pid, string exe, string srcRoot)
		{
			var process = GetProcess(pid);
			if (process != null) {
				log.DebugFormat("По завершении будет запущен процесс {0}", exe);
			}
			else {
				log.WarnFormat("Процесс {0} не найден", pid);
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

			var task = new Task<Process>(() => {
				Update(pid, exe, srcRoot);
				return Start(exe);
			});
			task.Start();
			return task;
		}

		private Process Start(string exe)
		{
			if (String.IsNullOrEmpty(exe)) {
				log.Error("Не указан исполняемый файл для запуска");
				return null;
			}
			var arguments = "import";
			log.DebugFormat("запускаю процесс {0} {1}", exe, arguments);
			return Process.Start(new ProcessStartInfo(exe, arguments) {
				WorkingDirectory = Path.GetDirectoryName(exe)
			});
		}

		public void Update(int pid, string exe, string srcRoot)
		{
			WaitForPid(pid);

			var destRootPath = Path.GetDirectoryName(exe);

			var selfExe = Path.GetFileName(typeof(MainWindow).Assembly.Location);
			var version = File.ReadAllText(Path.Combine(srcRoot, "version.txt")).Trim();
			var newBinPath = Path.Combine(destRootPath, version);
			var oldBinPath = Path.Combine(destRootPath, "bin");
			var exePath = destRootPath;
			var files = Directory.GetFiles(srcRoot);

			var ignore = new[] {selfExe, selfExe + ".config", "version.txt"};

			files = files.Except(ignore, new FileNameComparer()).ToArray();
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
		}

		private void WaitForPid(int pid)
		{
			var process = GetProcess(pid);
			if (process == null)
				return;

			var waitTimeout = TimeSpan.FromSeconds(30);
			log.DebugFormat("Жду завершения процесса {0}", process.Id);
			var exited = process.WaitForExit((int)waitTimeout.TotalMilliseconds);
			if (!exited) {
				try {
					log.ErrorFormat("Процесс {0} не завершился за {1}, завершаю принудительно", pid, waitTimeout);
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
			catch (ArgumentException) {
				//если процесс уже завершился
			}
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using BsDiff;
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
		private static ILog log = LogManager.GetLogger(typeof(MainWindow));

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
				try {
					new WindowInteropHelper(this) {
						Owner = process.MainWindowHandle
					};
				} catch(Exception e) {
					log.Warn("Не удалось получить указатель на главное окно приложения", e);
				}
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

		public static void Update(int pid, string exe, string srcRoot)
		{
			WaitForPid(pid);

			var dstRoot = Path.GetDirectoryName(exe);
			var selfExe = Path.GetFileName(typeof(MainWindow).Assembly.Location);
			var files = Directory.GetFiles(srcRoot);
			var ignore = new[] {selfExe, selfExe + ".config", "version.txt", "delete.me"};
			files = files.Except(ignore, new FileNameComparer()).ToArray();

			//убираем мусор
			//защита от дурака мусор будем убирать только если есть исполняемый файл
			//иначе нечего будем запускать
			var forDelete = Path.Combine(srcRoot, "delete.me");
			if (File.Exists(forDelete)
				&& File.Exists(Path.Combine(srcRoot, Path.GetFileName(exe)))) {
				var fordelete = File.ReadAllLines(forDelete).SelectMany(l => Directory.GetFiles(dstRoot, l)).ToArray();
				fordelete.Each(f => SafeFromAv(() => File.Delete(f)));
			}

			CopyFiles(files, dstRoot);
		}

		//на xp если выключить касперский endpoint security
		//при обращении к файлам будет ошибка доступ запрещен но если попробовать несколько раз то жизнь наладится
		private static void SafeFromAv(Action action)
		{
			var index = 0;
			while (true) {
				try {
					index++;
					action();
					break;
				}
				catch (Exception) {
					if (index > 3)
						throw;
					else
						Thread.Sleep(50);
				}
			}
		}

		private static void WaitForPid(int pid)
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

		private static void CopyFiles(IEnumerable<string> sources, string path)
		{
			foreach (var source in sources) {
				if (Path.GetExtension(source).Match(".bsdiff")) {
					var dst = Path.Combine(path, Path.GetFileNameWithoutExtension(source));
					var src = dst + ".old";
					log.Debug($"Применяю патч {source} > {dst}");
					SafeFromAv(() => File.Delete(src));
					SafeFromAv(() => File.Move(dst, src));
					using (var srcStream = File.OpenRead(src))
					using (var dstStream = File.Create(dst))
						BinaryPatchUtility.Apply(srcStream, () => File.OpenRead(source), dstStream);
					SafeFromAv(() => File.Delete(src));
				} else {
					var dst = Path.Combine(path, Path.GetFileName(source));
					log.Debug($"Копирую {source} > {dst}");
					SafeFromAv(() => File.Copy(source, dst, true));
				}
			}
		}
	}
}

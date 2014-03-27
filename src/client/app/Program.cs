using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using log4net.ObjectRenderer;
using log4net.Repository.Hierarchy;
using NDesk.Options;
using ReactiveUI;
using LogManager = log4net.LogManager;

namespace AnalitF.Net.Client
{
	public class ExceptionRenderer : IObjectRenderer
	{
		public void RenderObject(RendererMap rendererMap, object obj, TextWriter writer)
		{
			var ex = obj as ReflectionTypeLoadException;
			if (ex == null)
				return;
			foreach (var loaderException in ex.LoaderExceptions) {
				writer.WriteLine(loaderException);
			}
		}
	}

	public class Program
	{
		private static ILog log = LogManager.GetLogger(typeof(Program));

		public const uint ATTACH_PARENT_PROCESS = 0x0ffffffff;

		[DllImport("kernel32", SetLastError = true)]
		public static extern bool AttachConsole(uint dwProcessId);

		[STAThread]
		public static int Main(string[] args)
		{
			SingleInstance instance = null;
			var help = false;
			var version  = false;
			var quiet = false;
			var faultInject = false;
			var debugpipe = "";
			int result;
			try {
				XmlConfigurator.Configure();
				log.DebugFormat("Приложение запущено {0}", typeof(Program).Assembly.Location);
				log.Logger.Repository.RendererMap.Put(typeof(ReflectionTypeLoadException), new ExceptionRenderer());

				var options = new OptionSet {
					{"help", "Показать справку", v => help = v != null},
					{"version", "Показать информацию о версии", v => version = v != null},
					{"quiet", "Не выводить предупреждения при запуске", v => quiet = v != null},
#if DEBUG
					{"fault-inject", "", v => faultInject = v != null},
					{"debug-pipe=", "", v => debugpipe = v},
#endif
				};
				var cmds = options.Parse(args);

				//по умолчанию у gui приложения нет консоли, но если кто то запустил из консоли
				//то нужно с ним говорить
				AttachConsole(ATTACH_PARENT_PROCESS);

				if (help) {
					options.WriteOptionDescriptions(Console.Out);
					return 0;
				}

				if (version) {
					var assembly = typeof(Program).Assembly;
					Console.WriteLine(assembly.GetName().Version.ToString());
					var hash = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)
						.OfType<AssemblyCopyrightAttribute>()
						.Select(a => a.Copyright)
						.FirstOrDefault();
					Console.WriteLine(hash);
					return 0;
				}

				if (quiet) {
					var repository = (Hierarchy)LogManager.GetRepository();
					if (!repository.GetAppenders().OfType<ConsoleAppender>().Any()) {
						var layout = new PatternLayout("%d{dd.MM.yyyy HH:mm:ss.fff} [%t] %-5p %c - %m%n");
						layout.ActivateOptions();
						var consoleAppender = new ConsoleAppender();
						consoleAppender.Layout = layout;
						consoleAppender.ActivateOptions();
						repository.Root.AddAppender(consoleAppender);
					}
				}

				instance = new SingleInstance(typeof(AppBootstrapper).Assembly.GetName().Name);
				if (!instance.TryStart())
					return 0;

				RxApp.MessageBus.Listen<string>()
					.Where(m => m == "Startup")
					.Subscribe(_ => instance.SignalStartup());

				RxApp.MessageBus.Listen<string>()
					.Where(m => m == "Shutdown")
					.Subscribe(_ => instance.SignalShutdown());

				var splash = new SplashScreen(@"assets/images/splash.png");
				if (!quiet) {
					try {
						splash.Show(true);
					}
					catch(Exception e) {
						log.Error("Ошибка при отображение заставки", e);
					}
				}

				instance.Wait();

				var app = new App {
					Splash = splash,
					FaultInject = faultInject
				};
				app.InitializeComponent();
				var bootstapper = new AppBootstrapper();
				bootstapper.Config.Quiet = quiet;
				bootstapper.Config.DebugPipeName = debugpipe;
				bootstapper.Config.Cmd = cmds.FirstOrDefault();
				bootstapper.Start();
				result = app.Run();
				log.DebugFormat("Приложение завершено");
			}
			catch(EndUserError e) {
				result = 1;
				log.Error("Ошибка при запуске приложения", e);
				if (!quiet) {
					MessageBox.Show(
						e.Message,
						"АналитФАРМАЦИЯ: Внимание",
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
				}
			}
			catch(Exception e) {
				log.Error("Ошибка при запуске приложения", e);
				result = 1;
			}
			finally {
				if (instance != null)
					instance.SignalShutdown();
			}

			return result;
		}
	}
}
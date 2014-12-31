using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.PlatformServices;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using log4net.ObjectRenderer;
using log4net.Repository.Hierarchy;
using Microsoft.Win32.SafeHandles;
using NDesk.Options;
using ReactiveUI;
using ReactiveUI.Routing;
using LogManager = log4net.LogManager;
//для ilmerge, что бы найти ресурсы Xceed.Wpf.Toolkit
[assembly: ThemeInfo(ResourceDictionaryLocation.SourceAssembly, ResourceDictionaryLocation.SourceAssembly)]

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
		public const int ERROR_ACCESS_DENIED = 5;

		[DllImport("kernel32", SetLastError = true)]
		public static extern bool AttachConsole(uint dwProcessId);

		[DllImport("kernel32",SetLastError=true)]
		public static extern bool AllocConsole();

		[STAThread]
		public static int Main(string[] args)
		{
			//по умолчанию у gui приложения нет консоли, но если кто то запустил из консоли
			//то нужно с ним говорить
			//если делать это позже то вызов не дает результата
			AttachConsole(ATTACH_PARENT_PROCESS);
			SingleInstance instance = null;
			var help = false;
			var version  = false;
			var quiet = false;
			var faultInject = false;
			var debugpipe = "";
			int result;
			try {
				//проверка для ilmerge
				var merged = new [] {
					"Caliburn.Micro", "Xceed.Wpf.Toolkit", "System.Windows.Interactivity", "log4net", "Devart.Data", "Devart.Data.MySql"
				};
				AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) => {
					if (merged.Any(n => eventArgs.Name.StartsWith(n)))
						return typeof(Program).Assembly;
					return null;
				};

				var logConfig = new FileInfo(FileHelper.MakeRooted("log4net.config"));
				if (logConfig.Exists)
					XmlConfigurator.Configure(logConfig);
				else
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
				if (help) {
					options.WriteOptionDescriptions(Console.Out);
					return 0;
				}

				if (version) {
					var assembly = typeof(Program).Assembly;
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

				PlatformEnlightenmentProvider.Current = new CurrentPlatformEnlightenmentProvider();
				//регистрация объектов reactiveui в нормальной жизни это должно произойти автоматический
				//но после ilmerge логика регистрации будет сломана
				new ReactiveUI.Routing.ServiceLocationRegistration().Register();
				new ReactiveUI.Xaml.ServiceLocationRegistration().Register();

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
			catch(Exception e) {
				var message = e is EndUserError ? e.Message : e.ToString();
				result = 1;
				Console.WriteLine(message);
				log.Error("Ошибка при запуске приложения", e);
				if (!quiet) {
					MessageBox.Show(
						message,
						"АналитФАРМАЦИЯ: Внимание",
						MessageBoxButton.OK,
						MessageBoxImage.Error);
				}
			}
			finally {
				if (instance != null)
					instance.SignalShutdown();
			}

			return result;
		}
	}
}
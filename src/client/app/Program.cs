using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Printing;
using System.Reactive.Linq;
using System.Reactive.PlatformServices;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools;
using Common.Tools.Helpers;
using Devart.Data.MySql;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.ObjectRenderer;
using log4net.Repository.Hierarchy;
using Microsoft.Win32.SafeHandles;
using NDesk.Options;
using ReactiveUI;
using ReactiveUI.Routing;
using LogManager = log4net.LogManager;
using MessageBox = System.Windows.MessageBox;

//для ilmerge, что бы найти ресурсы Xceed.Wpf.Toolkit
[assembly: ThemeInfo(ResourceDictionaryLocation.SourceAssembly, ResourceDictionaryLocation.SourceAssembly)]

namespace AnalitF.Net.Client
{
	public class ExceptionRenderer : IObjectRenderer
	{
		public void RenderObject(RendererMap rendererMap, object obj, TextWriter writer)
		{
			var ex = obj as ReflectionTypeLoadException;
			if (ex != null) {
				foreach (var loaderException in ex.LoaderExceptions) {
					writer.WriteLine(loaderException);
				}
			}

			var db = obj as MySqlException;
			if (db != null) {
				writer.Write($"ErrorCode = {db.Code}, ");
				writer.Write(db.ToString());
			}

			var e = obj as Exception;
			if (e != null) {
				var mysql = e.Chain().OfType<MySqlException>().FirstOrDefault();
				if (mysql != null) {
					writer.Write($"MySql ErrorCode = {mysql.Code}, ");
				}
				writer.Write(e);
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

		[DllImport("kernel32", SetLastError=true)]
		public static extern bool AllocConsole();

		[DllImport("kernel32", SetLastError=true)]
		public static extern bool FreeConsole();

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
			var debug = new List<string>();
			var console = false;
			var encoding = "";
			int result;
			try {
				//проверка для ilmerge
				var merged = new [] {
					"Caliburn.Micro", "Xceed.Wpf.Toolkit", "System.Windows.Interactivity", "log4net",
					"Devart.Data", "Devart.Data.MySql",
					"WpfAnimatedGif",
					"AnalitF.Net.Client"
				};
				AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) => {
					var name = new AssemblyName(eventArgs.Name);
					if (merged.Any(n => name.Name.Equals(n, StringComparison.CurrentCultureIgnoreCase)))
						return typeof(Program).Assembly;
					return null;
				};

				var logConfig = new FileInfo(FileHelper.MakeRooted("log4net.config"));
				if (logConfig.Exists)
					XmlConfigurator.Configure(logConfig);
				else
					XmlConfigurator.Configure();

				string batchFile = null;
				var migrate = false;
				var firstChance = false;
				var options = new OptionSet {
					{"help", "Показать справку", v => help = v != null},
					{"version", "Показать информацию о версии", v => version = v != null},
					{"quiet", "Не выводить предупреждения при запуске", v => quiet = v != null},
					{"console", v => console = v != null},
					{"encoding=", v => encoding = v},
					{"debug=", v => debug.Add(v) },
					{"first-chance", v => firstChance = v != null },
					{"batch=", "Запустить приложение и вызвать автозаказ с указанным файлом", v => batchFile = v},
					{"i", "", v => migrate = true},
#if DEBUG
					{"fault-inject", "", v => faultInject = v != null},
					{"debug-pipe=", "", v => debugpipe = v},
#endif
				};
				var cmds = options.Parse(args);

				if (firstChance) {
					AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) => {
						log.Error(new StackTrace());
						log.Error("FirstChanceException", eventArgs.Exception);
						if (eventArgs.Exception is PrintQueueException) {
							var ex = (PrintQueueException)eventArgs.Exception;
							log.ErrorFormat($"Ошибка принтера '{ex.PrinterName}'");
						}
					};
				}
				//при определения установлен ли .net могла произойти ошибка и мы решили что .net установлен но на самом деле нет
				//или человек мог сменить компьютер
				//в этом случае при первом запуске с ключом миграции приложение свалится, и человек получит предложение установить .net
				//если он егу установит то при последующем запуске мы должны понять что нужно импортировать данные
				var files = new [] { "Params.txt", "Password.txt", "GlobalParams.txt" };
				if (files.All(x => File.Exists(FileHelper.MakeRooted(x)))) {
					migrate = true;
				}
				if (migrate)
					cmds = new List<string> { "migrate" };

				if (!String.IsNullOrEmpty(encoding)) {
					Console.OutputEncoding = Encoding.GetEncoding(encoding);
				}

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
					Console.WriteLine(assembly.GetName().Version);
					Console.WriteLine(hash);
					return 0;
				}

				var repository = (Hierarchy)LogManager.GetRepository();
				if (console) {
					if (!repository.GetAppenders().OfType<ConsoleAppender>().Any()) {
						var layout = new PatternLayout("%d{dd.MM.yyyy HH:mm:ss.fff} [%t] %-5p %c - %m%n");
						layout.ActivateOptions();
						var consoleAppender = new ConsoleAppender();
						consoleAppender.Layout = layout;
						consoleAppender.ActivateOptions();
						repository.Root.AddAppender(consoleAppender);
					}
				}

				foreach (var loggername in debug) {
					var logger = (Logger)repository.GetLogger(loggername);
					logger.Level = Level.Debug;
				}

				log.InfoFormat("Приложение запущено {0}", typeof(Program).Assembly.Location);
				try {
					log.InfoFormat("Версия операционной системы {0}", Environment.OSVersion);
					log.InfoFormat("Версия среды выполнения {0}", Environment.Version);
				}
				catch (Exception e) {
					log.Error("Не удалось получить информацию о версии среды или ос", e);
				}
				log.Logger.Repository.RendererMap.Put(typeof(Exception), new ExceptionRenderer());
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
				if (!String.IsNullOrEmpty(batchFile))
					bootstapper.Config.Cmd = "batch=" + batchFile;
				bootstapper.Start();
				result = app.Run();
				log.DebugFormat("Приложение завершено");
			}
			catch(OptionException e) {
				result = 1;
				Console.WriteLine(e.Message);
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
				instance?.SignalShutdown();
			}

			return result;
		}
	}
}

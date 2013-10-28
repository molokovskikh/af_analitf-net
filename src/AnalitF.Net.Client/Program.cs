using System;
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
using log4net.Config;
using NDesk.Options;
using ReactiveUI;
using LogManager = log4net.LogManager;

namespace AnalitF.Net.Client
{
	public class Program
	{
		private static ILog log = LogManager.GetLogger(typeof(Program));

		public const uint ATTACH_PARENT_PROCESS = 0x0ffffffff;

		[DllImport("kernel32", SetLastError = true)]
		public static extern bool AttachConsole(uint dwProcessId);

		[STAThread]
		public static void Main(string[] args)
		{
			SingleInstance instance = null;
			try {
				XmlConfigurator.Configure();

				var help = false;
				var version  = false;
				var quiet = false;
				var faultInject = false;
				var options = new OptionSet {
					{"help", "Показать справку", v => help = v != null},
					{"version", "Показать информацию о версии", v => version = v != null},
					{"quiet", "Не выводить предупреждения при запуске", v => quiet = v != null},
					{"fault-inject", "", v => faultInject = v != null}
				};
				options.Parse(args);

				//по умолчанию у gui приложения нет консоли, но если кто то запустил из консоли
				//то нужно с ним говорить
				AttachConsole(ATTACH_PARENT_PROCESS);

				if (help) {
					options.WriteOptionDescriptions(Console.Out);
					return;
				}

				if (version) {
					var assembly = typeof(Program).Assembly;
					Console.WriteLine(assembly.GetName().Version.ToString());
					var hash = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)
						.OfType<AssemblyCopyrightAttribute>()
						.Select(a => a.Copyright)
						.FirstOrDefault();
					Console.WriteLine(hash);
					return;
				}

				instance = new SingleInstance(typeof(AppBootstrapper).Assembly.GetName().Name);
				if (!instance.TryStart())
					return;

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
					Quiet = quiet,
					Splash = splash,
					FaultInject = faultInject
				};
				app.InitializeComponent();
				app.Run();
			}
			catch(EndUserError e) {
				MessageBox.Show(
					e.Message,
					"АналитФАРМАЦИЯ: Внимание",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
			catch(Exception e) {
				log.Error("Ошибка при запуске приложения", e);
			}
			finally {
				if (instance != null)
					instance.SignalShutdown();
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.NHibernate;
using Common.Tools;
using Newtonsoft.Json;
using ReactiveUI;
using log4net.Config;
using ILog = log4net.ILog;
using LogManager = Caliburn.Micro.LogManager;

namespace AnalitF.Net.Client
{
	public class AppBootstrapper : BootstrapperBase, IDisposable
	{
		private ILog log = log4net.LogManager.GetLogger(typeof(AppBootstrapper));
		private bool FailFast;
		private SingleInstance instance;
		private bool import;
		private string name;
#if DEBUG
		private DebugPipe debugPipe;
#endif

		public string SettingsPath;
		public ShellViewModel Shell;

		private static bool isUiInitialized;

		public static Config.Initializers.NHibernate NHibernate;
		public Config.Config Config = new Config.Config();

		public AppBootstrapper()
			: this(true)
		{
		}

		public AppBootstrapper(bool useApplication = true, string name = null)
			: base(useApplication)
		{
			FailFast = !useApplication;
			this.name = name ?? typeof(AppBootstrapper).Assembly.GetName().Name;
			instance = new SingleInstance(this.name);
			SettingsPath = this.name + ".data";

			Start();
		}

		public bool IsInitialized { get; private set; }

		private void InitLog()
		{
			if (FailFast)
				return;

			LogManager.GetLog = t => new Log4net(t);
			TaskScheduler.UnobservedTaskException += (sender, args) => {
				args.SetObserved();
				log.Error("Ошибка пир выполнении задачи", args.Exception.GetBaseException());
			};
			AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
				log.Error("Ошибка в приложении", args.ExceptionObject as Exception);
				CheckShutdown(args.ExceptionObject as Exception);
			};
		}

		protected override void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			if (FailFast)
				return;
			log.Error("Ошибка в главной нитки приложения", e.Exception);
			e.Handled = true;
			CheckShutdown(e.Exception);
		}

		private void CheckShutdown(Exception e)
		{
			if (!IsInitialized) {
				//если не запустились то нужно сказать что случилась беда
				//если запуск состоялся просто проглатываем исключение
				var message = ErrorHelper.TranslateException(e)
					?? String.Format("Не удалось запустить приложение из-за ошибки {0}", e.Message);
				if (Application != null && ((App)Application).Quiet)
					Console.WriteLine(message);
				else
					MessageBox.Show(
						message,
						"АналитФАРМАЦИЯ: Внимание",
						MessageBoxButton.OK,
						MessageBoxImage.Warning);

				Application.Current.Shutdown();
			}
		}

		protected override void OnExit(object sender, EventArgs e)
		{
			Serialize();
		}

		protected override void OnStartup(object sender, StartupEventArgs e)
		{
			Init();
		}

		public void Init()
		{
			InitLog();
			if (!InitApp()) {
				Application.Current.Shutdown();
				return;
			}

			if (Application != null)
				((App)Application).RegisterResources();
			InitUi();
			InitDb();
			InitShell();
		}

		public void InitShell()
		{
			var windowManager = IoC.Get<IWindowManager>();
			Shell = (ShellViewModel) IoC.GetInstance(typeof(ShellViewModel), null);
			Shell.Config = Config;
			if (Application != null)
				Shell.Quiet = ((App)Application).Quiet;

			Deserialize();

			windowManager.ShowWindow(Shell, null, new Dictionary<string, object> {
				{"WindowState", WindowState.Maximized}
			});
			IsInitialized = true;
		}

		public void Serialize()
		{
			try {
				if (!IsInitialized)
					return;

				using(var stream = new StreamWriter(SettingsPath)) {
					var serializer = new JsonSerializer {
						ContractResolver = new NHibernateResolver()
					};
					serializer.Serialize(stream, Shell);
				}
			}
			catch (Exception e) {
				log.Error("Не удалось сохранить настройки", e);
			}
		}

		public void Deserialize()
		{
			try
			{
				if (!File.Exists(SettingsPath))
					return;

				Shell.IsNotifying = false;
				using(var stream = new StreamReader(SettingsPath)) {
					var serializer = new JsonSerializer {
						ContractResolver = new NHibernateResolver()
					};
					serializer.Populate(stream, Shell);
				}
			}
			catch(Exception e) {
				log.Error("Не удалось прочитать настройки", e);
			}
			finally {
				Shell.IsNotifying = true;
			}
		}

		protected override object GetInstance(Type service, string key)
		{
			if (typeof(IWindowManager) == service)
				return new Extentions.WindowManager();
			return base.GetInstance(service, key);
		}

		protected override void BuildUp(object instance)
		{
			if (instance == null)
				return;

			base.BuildUp(instance);

			var shell = instance.GetType().GetField("Shell");
			if (shell != null)
				shell.SetValue(instance, Shell);

			var manager = instance.GetType().GetField("Manager");
			if (manager != null)
				manager.SetValue(instance, GetInstance(typeof(IWindowManager), null));
		}

		private bool InitApp()
		{
			var isSingle = instance.Check();
			if (!isSingle)
				return false;

			var args = Environment.GetCommandLineArgs();
			Config.BaseUrl = new Uri(ConfigurationManager.AppSettings["Uri"]);
			Config.RootDir = FileHelper.MakeRooted(Config.RootDir);
			Config.TmpDir = FileHelper.MakeRooted(Config.TmpDir);
			Config.DbDir = FileHelper.MakeRooted(Config.DbDir);

#if DEBUG
			debugPipe = new DebugPipe(args);
#endif
			SettingsPath = FileHelper.MakeRooted(SettingsPath);
			import = args.LastOrDefault().Match("import");

			if (Directory.Exists(Config.TmpDir)) {
				if (!import) {
					try {
						Directory.Delete(Config.TmpDir, true);
						Directory.CreateDirectory(Config.TmpDir);
					}
					catch(Exception) {}
				}
			}
			else {
				Directory.CreateDirectory(Config.TmpDir);
			}

			return true;
		}

		private void InitDb()
		{
			if (NHibernate == null) {
				NHibernate = new Config.Initializers.NHibernate();
				NHibernate.Init();
			}

			new SanityCheck(Config.DbDir).Check(import);
		}

		public static void InitUi()
		{
			//в тестах мы можем дважды инициализировать ui
			//это приведет к тому что делегаты будут вызываться рекурсивно
			if (isUiInitialized)
				return;

			new Config.Initializers.Caliburn().Init();

			isUiInitialized = true;
		}

		public void Dispose()
		{
			instance.Dispose();
		}
	}
}
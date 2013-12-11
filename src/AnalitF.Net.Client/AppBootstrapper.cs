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
		private bool isImport;
		private string name;
#if DEBUG
		private DebugPipe debugPipe;
#endif

		public string SettingsPath;
		public ShellViewModel Shell;

		public static Config.Initializers.Caliburn Caliburn;
		public static Config.Initializers.NHibernate NHibernate;
		public Config.Config Config = new Config.Config();
		public string[] Args = new string[0];
		public string DebugPipeName;

		public AppBootstrapper()
			: this(true)
		{
		}

		public AppBootstrapper(bool useApplication = true, bool start = true, string name = null)
			: base(useApplication)
		{
			FailFast = !useApplication;
			this.name = name ?? typeof(AppBootstrapper).Assembly.GetName().Name;
			SettingsPath = this.name + ".data";

			if (start)
				Start();
		}

		public bool IsInitialized { get; private set; }

		private void InitLog()
		{
			if (FailFast) {
				LogManager.GetLog = t => new FailFastLog(t);
				return;
			}

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
				//нужно закрыть заставку прежде чем показывать сообщение
				//иначе окно с сообщение будет закрыто и не отобразится
				var app = ((App)Application);
				if (app != null && app.Splash != null) {
						app.Splash.Close(TimeSpan.Zero);
				}
				//если не запустились то нужно сказать что случилась беда
				//если запуск состоялся просто проглатываем исключение
				var message = ErrorHelper.TranslateException(e)
					?? String.Format("Не удалось запустить приложение из-за ошибки: {0}", e.Message);
				if (Config.Quit)
					Console.Out.WriteLine(message);
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
			RxApp.MessageBus.SendMessage("Shutdown");
		}

		protected override void OnStartup(object sender, StartupEventArgs e)
		{
			Init();
		}

		public void Init()
		{
			InitLog();
			InitApp();

			var app = ((App)Application);
			if (app != null) {
				app.RegisterResources();
				if (app.FaultInject)
					throw new Exception("Ошибка при инициализации");
			}

			InitUi();
			InitDb();
			InitShell();
		}

		public void InitShell()
		{
			var windowManager = IoC.Get<IWindowManager>();
			Shell = (ShellViewModel) IoC.GetInstance(typeof(ShellViewModel), null);
			Shell.Config = Config;
			Shell.IsImport = isImport;
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

			Util.SetValue(instance, "Manager", GetInstance(typeof(IWindowManager), null));
			Util.SetValue(instance, "Shell", Shell);
			Util.SetValue(instance, "Env", Shell.Env);
		}

		private void InitApp()
		{
			Config.BaseUrl = new Uri(ConfigurationManager.AppSettings["Uri"]);
			Config.RootDir = FileHelper.MakeRooted(Config.RootDir);
			Config.TmpDir = FileHelper.MakeRooted(Config.TmpDir);
			Config.DbDir = FileHelper.MakeRooted(Config.DbDir);
#if DEBUG
			debugPipe = new DebugPipe(DebugPipeName);
#endif
			SettingsPath = FileHelper.MakeRooted(SettingsPath);
			isImport = Args.Any(a => a.Match("import"));

			if (Directory.Exists(Config.TmpDir)) {
				if (!isImport) {
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
		}

		private void InitDb()
		{
			if (NHibernate != null)
				return;

			NHibernate = new Config.Initializers.NHibernate();
			NHibernate.Init();
			new SanityCheck(Config.DbDir).Check(isImport);
		}

		public static void InitUi()
		{
			//в тестах мы можем дважды инициализировать ui
			//это приведет к тому что делегаты будут вызываться рекурсивно
			if (Caliburn != null)
				return;

			Caliburn = new Config.Initializers.Caliburn();
			Caliburn.Init();
		}

		public void Dispose()
		{
			if (Shell != null)
				Shell.Dispose();
		}
	}
}
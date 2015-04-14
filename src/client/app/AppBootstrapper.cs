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
using System.Windows.Media;
using System.Windows.Threading;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;
using Common.NHibernate;
using Common.Tools;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;
using log4net.Config;
using ILog = log4net.ILog;
using LogManager = Caliburn.Micro.LogManager;

namespace AnalitF.Net.Client
{
	public class AppBootstrapper : BootstrapperBase, IDisposable
	{
		public Func<object, DependencyObject, object, UIElement> DefaultLocateForModel;

		private ILog log = log4net.LogManager.GetLogger(typeof(AppBootstrapper));
		private bool FailFast;
#if DEBUG
		private DebugPipe debugPipe;
#endif

		public ShellViewModel Shell;

		public static Config.Initializers.Caliburn Caliburn;
		public static Config.Initializers.NHibernate NHibernate;
		public Config.Config Config = new Config.Config();

		public AppBootstrapper()
			: this(true)
		{
		}

		public AppBootstrapper(bool useApplication = true)
			: base(useApplication)
		{
			FailFast = !useApplication;
			DefaultLocateForModel = ViewLocator.LocateForModel;
			ViewLocator.LocateForModel = LocateForModel;
		}

		public bool IsInitialized { get; private set; }

		private void InitLog()
		{
			if (FailFast) {
				LogManager.GetLog = t => new FailFastLog(t);
				return;
			}

			//нужно вызвать иначе wpf игнорирует все настройки протоколирование
			PresentationTraceSources.Refresh();
			//ошибки которые возникают при биндинге wpf проглатывает это могут быть как безобидные ошибки
			//например не удалось преобразовать строку в число так и критические
			PresentationTraceSources.DataBindingSource.Listeners.Add(new DelegateTraceListner(m => log.Error(m)));

			LogManager.GetLog = t => new Log4net(t);
			TaskScheduler.UnobservedTaskException += (sender, args) => {
				if (!FailFast)
					args.SetObserved();
				log.Error("Ошибка при выполнении задачи", args.Exception.GetBaseException());
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
				if (!Config.Quiet)
					MessageBox.Show(
						message,
						"АналитФАРМАЦИЯ: Внимание",
						MessageBoxButton.OK,
						MessageBoxImage.Warning);

				Application.Current.Shutdown(1);
			}
		}

		protected override void OnExit(object sender, EventArgs e)
		{
			Serialize();
			RxApp.MessageBus.SendMessage("Shutdown");
		}

		protected override void OnStartup(object sender, StartupEventArgs e)
		{
			InitLog();
			InitApp();
			var app = ((App)Application);
			if (app != null) {
				if (app.FaultInject)
					throw new Exception("Ошибка при инициализации");
				app.RegisterResources();
			}
			InitUi(FailFast);
			InitDb();
			InitShell();
		}

		public void InitShell()
		{
			try {
				if (Application != null) {
					using(var session = NHibernate.Factory.OpenSession()) {
						StyleHelper.BuildStyles(Application.Resources, session.Query<CustomStyle>());
					}
				}
			}
			catch(Exception e) {
				log.Error("Не удалось инициализировать стили", e);
			}
			var windowManager = IoC.Get<IWindowManager>();
			Shell = (ShellViewModel) IoC.GetInstance(typeof(ShellViewModel), null);
			Shell.Config = Config;
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

				if (String.IsNullOrEmpty(Config.SettingsPath))
					return;

				using(var stream = new StreamWriter(Config.SettingsPath)) {
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
				if (!File.Exists(Config.SettingsPath))
					return;

				Shell.IsNotifying = false;
				using(var stream = new StreamReader(Config.SettingsPath)) {
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

		public void InitApp()
		{
			if (ConfigurationManager.AppSettings["Uri"] != null)
				Config.BaseUrl = new Uri(ConfigurationManager.AppSettings["Uri"]);

			Config.RootDir = FileHelper.MakeRooted(Config.RootDir);
#if DEBUG
			debugPipe = new DebugPipe(Config.DebugPipeName);
#endif

			if (Directory.Exists(Config.TmpDir)) {
				try {
					//перемещаем лог файл обновления что отправить его на сервер
					if (Directory.Exists(Config.BinUpdateDir)) {
						Directory.GetFiles(Config.BinUpdateDir, "*.log")
							.Each(f => File.Move(f, FileHelper.Uniq(Path.Combine(Config.RootDir, Path.GetFileName(f)))));
					}
				}
				catch(Exception) {}
				if (!Config.Cmd.Match("import")) {
					try {
						Directory.Delete(Config.TmpDir, true);
						Directory.CreateDirectory(Config.TmpDir);
					}
					catch(Exception) {}
				}
				//в одной из версий временные файлы складывались в корень, чистим мусор
				try {
					Directory.GetFiles(Config.RootDir, "*.xls").Each(f => File.Delete(f));
				}
				catch (Exception) {}
			}
			else {
				Directory.CreateDirectory(Config.TmpDir);
			}
		}

		private void InitDb()
		{
			if (NHibernate != null)
				return;

			//если сборки обединены то логика определения системы протоколирование не работает
			//нужно вручную настроить ее
			LoggerProvider.SetLoggersFactory(new Log4NetLoggerFactory());
			NHibernate = new Config.Initializers.NHibernate();
			NHibernate.Init();
			var sanityCheck = new SanityCheck();
			sanityCheck.Config = Config;
			sanityCheck.Check(Config.Cmd.Match("import"));
		}

		public static void InitUi(bool failfast)
		{
			//в тестах мы можем дважды инициализировать ui
			//это приведет к тому что делегаты будут вызываться рекурсивно
			if (Caliburn != null)
				return;

			Caliburn = new Config.Initializers.Caliburn();
			Caliburn.Init(failfast);
		}

		public UIElement LocateForModel(object model, DependencyObject displayLocation, object context)
		{
			if (model is WaybillDetails) {
				var waybills = (WaybillDetails)model;
				waybills.SkipRestoreTable = true;
				return new WaybillDetailsView(waybills);
			}
			return DefaultLocateForModel(model, displayLocation, context);
		}

		public void Dispose()
		{
			if (Shell != null)
				Shell.Dispose();
		}
	}
}
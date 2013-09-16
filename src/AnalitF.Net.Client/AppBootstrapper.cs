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
		private bool catchExceptions;
		private SingleInstance instance;
		private bool import;
		private string name;
#if DEBUG
		private DebugPipe debugPipe;
#endif

		public string SettingsPath;
		public ShellViewModel Shell;

		private static bool IsUiInitialized;

		public static Config.Initializers.NHibernate NHibernate;
		public static string DataPath = "data";
		public static string TempPath = "temp";

		public AppBootstrapper()
			: this(true)
		{
		}

		public AppBootstrapper(bool useApplication = true, string name = null)
			: base(useApplication)
		{
			Start();
			catchExceptions = useApplication;
			this.name = name ?? typeof(AppBootstrapper).Assembly.GetName().Name;
			instance = new SingleInstance(this.name);
			SettingsPath = this.name + ".data";
		}

		public bool IsInitialized { get; private set; }

		private void InitLog()
		{
			if (!catchExceptions)
				return;

			LogManager.GetLog = t => new Log4net(t);
			AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
				log.Error("Ошибка в приложении", args.ExceptionObject as Exception);
				CheckShutdown(args.ExceptionObject as Exception);
			};
		}

		protected override void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			if (!catchExceptions)
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

		private bool InitApp()
		{
			var isSingle = instance.Check();
			if (!isSingle)
				return false;

			var args = Environment.GetCommandLineArgs();
#if DEBUG
			debugPipe = new DebugPipe(args);
#endif
			TempPath = FileHelper.MakeRooted(TempPath);
			DataPath = FileHelper.MakeRooted(DataPath);
			SettingsPath = FileHelper.MakeRooted(SettingsPath);

			import = args.LastOrDefault().Match("import");

			if (Directory.Exists(TempPath)) {
				if (!import) {
					try {
						Directory.Delete(TempPath, true);
						Directory.CreateDirectory(TempPath);
					}
					catch(Exception) {}
				}
			}
			else {
				Directory.CreateDirectory(TempPath);
			}

			Tasks.BaseUri = new Uri(ConfigurationManager.AppSettings["Uri"]);
			Tasks.ArchiveFile = Path.Combine(TempPath, "archive.zip");
			Tasks.ExtractPath = Path.Combine(TempPath, "update");
			Tasks.RootPath = FileHelper.MakeRooted(".");
			return true;
		}

		private void InitDb()
		{
			if (NHibernate == null) {
				NHibernate = new Config.Initializers.NHibernate();
				NHibernate.Init();
			}

			new SanityCheck(DataPath).Check(import);
		}

		public static void InitUi()
		{
			//в тестах мы можем дважды инициализировать ui
			//это приведет к тому что делегаты будут вызываться рекурсивно
			if (IsUiInitialized)
				return;

			MessageBus.Current.RegisterScheduler<string>(ImmediateScheduler.Instance);
			//нужно затем что бы можно было делать модели без суффикса ViewModel
			//достаточно что бы они лежали в пространстве имен ViewModels
			ViewLocator.NameTransformer.AddRule(
				@"(?<nsbefore>([A-Za-z_]\w*\.)*)(?<subns>ViewModels\.)"
				+ @"(?<nsafter>([A-Za-z_]\w*\.)*)(?<basename>[A-Za-z_]\w*)"
				+ @"(?!<suffix>ViewModel)$",
				"${nsbefore}Views.${nsafter}${basename}View");
			//что бы не нужно было использовать суффиксы View и ViewModel
			ViewLocator.NameTransformer.AddRule(
				@"(?<nsbefore>([A-Za-z_]\w*\.)*)(?<subns>ViewModels\.)"
				+ @"(?<nsafter>([A-Za-z_]\w*\.)*)(?<basename>[A-Za-z_]\w*)"
				+ @"(?!<suffix>)$",
				"${nsbefore}Views.${nsafter}${basename}");

			ContentElementBinder.Register();
			SaneCheckboxEditor.Register();
			NotifyValueSupport.Register();

			var customPropertyBinders = new Action<IEnumerable<FrameworkElement>, Type>[] {
				EnabledBinder.Bind,
				VisibilityBinder.Bind,
			};
			var customBinders = new Action<Type, IEnumerable<FrameworkElement>, List<FrameworkElement>>[] {
				//сначала должен обрабатываться поиск и только потом переход
				SearchBinder.Bind,
				EnterBinder.Bind,
			};

			var defaultBindProperties = ViewModelBinder.BindProperties;
			var defaultBindActions = ViewModelBinder.BindActions;
			var defaultBind = ViewModelBinder.Bind;
			ViewModelBinder.Bind = (viewModel, view, context) => {
				defaultBind(viewModel, view, context);
				ContentElementBinder.Bind(viewModel, view, context);
				CommandBinder.Bind(viewModel, view, context);
				FocusBehavior.Bind(viewModel, view, context);
			};

			ViewModelBinder.BindProperties = (elements, type) => {
				foreach (var binder in customPropertyBinders) {
					binder(elements, type);
				}
				return defaultBindProperties(elements, type);
			};

			ViewModelBinder.BindActions = (elements, type) => {
				var binded = defaultBindActions(elements, type).ToList();

				foreach (var binder in customBinders) {
					binder(type, elements, binded);
				}
				return elements;
			};
			IsUiInitialized = true;
		}

		public void Dispose()
		{
			instance.Dispose();
		}
	}
}
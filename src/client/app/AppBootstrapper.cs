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
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;
using Common.NHibernate;
using Common.Tools;
using Common.Tools.Helpers;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;
using log4net.Config;
using ILog = log4net.ILog;
using LogManager = Caliburn.Micro.LogManager;
using WindowManager = AnalitF.Net.Client.Config.Caliburn.WindowManager;

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

		public static Config.Caliburn.Caliburn Caliburn;
		public static Config.NHibernate.NHibernate NHibernate;
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
			//если не запустились то нужно сказать что случилась беда
			//если запуск состоялся просто проглатываем исключение
			if (IsInitialized)
				return;

			//нужно закрыть заставку прежде чем показывать сообщение
			//иначе окно с сообщение будет закрыто и не отобразится
			var app = ((App)Application);
			if (app != null && app.Splash != null) {
				app.Splash.Close(TimeSpan.Zero);
			}
			//нужно вывалить все исключение тк человек всего скорее пришлет снимок экрана
			//и по нему нужно произвести диагностику
			var message = ErrorHelper.TranslateException(e)
				?? String.Format("Не удалось запустить приложение из-за ошибки: {0}", e);
			if (!Config.Quiet)
				MessageBox.Show(
					message,
					"АналитФАРМАЦИЯ: Внимание",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

			Application.Current.Shutdown(1);
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
				return new WindowManager();
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

		private void InitLog()
		{
			if (FailFast) {
				LogManager.GetLog = t => new FailFastLog(t);
				return;
			}

			//нужно вызвать иначе wpf игнорирует все настройки протоколирование
			//ошибки которые возникают при биндинге wpf проглатывает это могут быть как безобидные ошибки
			//например не удалось преобразовать строку в число так и критические
			//конфигурация должна быть в конфигурационном файле иначе все игнорируется
			PresentationTraceSources.Refresh();

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

			var count = 0;
			repeat:
			try {
				count++;
				//если это попытка восстановления нужно очистить
				if (Shell != null) {
					Shell.Dispose();
				}
				var windowManager = IoC.Get<IWindowManager>();
				Shell = new ShellViewModel(Config);
				Deserialize();
				windowManager.ShowWindow(Shell, null, new Dictionary<string, object> {
					{"WindowState", WindowState.Maximized}
				});

				if (Application != null) {
					//если это повторный запуск то мы можем потерять окно
					//в этом случа нужно найти потеряные окна и закрыть их
					//что бы избежать ситуации когда главное окно закрылось а приложение не завершилось
					//тк windows считает что у него еще есть активные окна
					//
					//мы не можем выполнять очистку вместе с очисткой предудущего главного экрана
					//тк если мы это сделаем windows попытается завершить процесс тк у него не будет больше окон
					var lostWindows = Application.Windows.OfType<Window>()
						.Where(x => x.DataContext is ShellViewModel && ((ShellViewModel)x.DataContext) != Shell);
					foreach (var window in lostWindows)
						window.Close();
				}
			}
			catch(Exception e) {
				log.Error("Ошибка при запуске приложения", e);
				if (count > 1 || !RepairDb.TryToRepair(e, Config)) {
					throw;
				}
				goto repeat;
			}
			IsInitialized = true;
		}

		private void InitDb()
		{
			if (NHibernate != null)
				return;

			NHibernate = new Config.NHibernate.NHibernate();
			NHibernate.Init();

			if (Config.Cmd.Match("repair")) {
				using(var cmd = new RepairDb(Config)) {
					cmd.Execute();
				}
			}

			var count = 0;
			repeat:
			try {
				count++;
				using (var sanityCheck = new SanityCheck(Config)) {
					sanityCheck.Check(Config.Cmd.Match("import"));
				}
			}
			catch(Exception e) {
				log.Error("Ошибка при запуске приложения", e);
				if (count > 1 || !RepairDb.TryToRepair(e, Config)) {
					throw;
				}
				goto repeat;
			}
		}

		public static void InitUi(bool failfast)
		{
			//в тестах мы можем дважды инициализировать ui
			//это приведет к тому что делегаты будут вызываться рекурсивно
			if (Caliburn != null)
				return;

			Caliburn = new Config.Caliburn.Caliburn();
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
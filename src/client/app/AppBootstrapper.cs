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
using Dapper.Contrib.Extensions;
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
		private ILog log = log4net.LogManager.GetLogger(typeof(AppBootstrapper));
		private bool FailFast;
#if DEBUG
		private DebugPipe debugPipe;
#endif

		public ShellViewModel Shell;

		public static Config.Caliburn.Caliburn Caliburn;
		public static Config.NHibernate.NHibernate NHibernate;
		public Config.Config Config = new Config.Config();
		private WindowManager windowManager;

		public AppBootstrapper()
			: this(true)
		{
		}

		public AppBootstrapper(bool useApplication = true)
			: base(useApplication)
		{
			FailFast = !useApplication;
		}

		public bool IsInitialized { get; private set; }

		protected override void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			if (FailFast)
				return;

			log.Error("Ошибка в главной нитки приложения", e.Exception);
			e.Handled = true;
			if (!CheckShutdown(e.Exception)) {
				if (ErrorHelper.IsDbCorrupted(e.Exception))
					windowManager?.Error(ErrorHelper.TranslateException(e.Exception));
			}
		}

		private bool CheckShutdown(Exception e)
		{
			//если не запустились то нужно сказать что случилась беда
			//если запуск состоялся просто проглатываем исключение
			if (IsInitialized)
				return false;

			//нужно закрыть заставку прежде чем показывать сообщение
			//иначе окно с сообщение будет закрыто и не отобразится
			((App)Application)?.Splash?.Close(TimeSpan.Zero);
			//нужно вывалить все исключение тк человек всего скорее пришлет снимок экрана
			//и по нему нужно произвести диагностику
			var message = ErrorHelper.TranslateException(e)
				?? $"Не удалось запустить приложение из-за ошибки: {e}";
			if (!Config.Quiet)
				MessageBox.Show(
					message,
					"АналитФАРМАЦИЯ: Внимание",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

			Application.Current.Shutdown(1);
			return true;
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
			Env.Current = new Env();
			InitShell();
		}

		public void Serialize()
		{
			try {
				if (!IsInitialized)
					return;

				if (String.IsNullOrEmpty(Config.SettingsPath))
					return;

				using(var stream = new StreamWriter(Config.SettingsPath))
					Shell.Serialize(stream);
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

				using(var stream = new StreamReader(Config.SettingsPath))
					Shell.Deserialize(stream);
			}
			catch(Exception e) {
				log.Error("Не удалось прочитать настройки", e);
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
			foreach (var key in ConfigurationManager.AppSettings.AllKeys) {
				var value = ConfigurationManager.AppSettings[key];
				try {
					var property = Config.GetType().GetField(key);
					if (property == null)
						continue;
					property.SetValue(Config, value);
				}
				catch(Exception e) {
#if DEBUG
					throw;
#else
					log.Warn(String.Format("Не удалось считать параметр '{0}'='{1}'", key, value), e);
#endif
				}
			}
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
				} catch(Exception e) {
					log.Warn(e);
				}
				if (!Config.Cmd.Match("import")) {
					try {
						FileHelper.DeleteDir(Config.TmpDir);
						Directory.CreateDirectory(Config.TmpDir);
					} catch(Exception e) {
						log.Warn(e);
					}
				}
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
				Shell?.Dispose();
				windowManager = (WindowManager)IoC.Get<IWindowManager>();
				Shell = new ShellViewModel(Config);
				Deserialize();
				windowManager.ShowWindow(Shell, null, new Dictionary<string, object> {
					{"WindowState", WindowState.Maximized}
				});

				if (Application != null) {
					//если это повторный запуск то мы можем потерять окно
					//в этом случае нужно найти потерянные окна и закрыть их
					//что бы избежать ситуации когда главное окно закрылось а приложение не завершилось
					//тк windows считает что у него еще есть активные окна
					//
					//мы не можем выполнять очистку вместе с очисткой предыдущего главного экрана
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

			SqlMapperExtensions.GetDatabaseType = x => "mysqlconnection";
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

		public void Dispose()
		{
			Shell?.Dispose();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.Tools;
using ReactiveUI;
using log4net.Config;
using ILog = log4net.ILog;
using LogManager = Caliburn.Micro.LogManager;

namespace AnalitF.Net.Client
{
	public class AppBootstrapper : Bootstrapper<ShellViewModel>
	{
		private ILog log = log4net.LogManager.GetLogger(typeof(AppBootstrapper));

		public static Config.Initializers.NHibernate NHibernate;

		public static string DataPath = "data";
		public static string TempPath = "temp";

		private bool isInitialized;
		private static bool Import;

		public AppBootstrapper()
		{
		}

		private void InitLog()
		{
			XmlConfigurator.Configure();
			LogManager.GetLog = t => new Log4net(t);
			AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
				log.Error("Ошибка в приложении", args.ExceptionObject as Exception);
			};
		}

		protected override void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			log.Error("Ошибка в главной нитки приложения", e.Exception);
			e.Handled = true;
			if (!isInitialized)
				Application.Current.Shutdown();
		}

		protected override void OnExit(object sender, EventArgs e)
		{
		}

		protected override void OnStartup(object sender, StartupEventArgs e)
		{
			InitLog();
			InitApp();
			InitUi();
			InitDb();

			base.OnStartup(sender, e);

			isInitialized = true;
		}

		protected override object GetInstance(Type service, string key)
		{
			if (typeof(IWindowManager) == service)
				return new Extentions.WindowManager();
			return base.GetInstance(service, key);
		}

		private static void InitApp()
		{
			TempPath = FileHelper.MakeRooted(TempPath);
			DataPath = FileHelper.MakeRooted(DataPath);

			var args = Environment.GetCommandLineArgs();
			Import = args.LastOrDefault().Match("import");

			if (Directory.Exists(TempPath)) {
				if (!Import) {
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

			Tasks.Uri = new Uri(ConfigurationManager.AppSettings["Uri"]);
			Tasks.ArchiveFile = Path.Combine(TempPath, "archive.zip");
			Tasks.ExtractPath = TempPath;
		}

		private static void InitDb()
		{
			NHibernate = new Config.Initializers.NHibernate();
			NHibernate.Init();

			new SanityCheck(DataPath).Check(Import);
		}

		public static void InitUi()
		{
			FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
				new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

			ContentElementBinder.RegisterConvention();
			var customPropertyBinders = new Action<IEnumerable<FrameworkElement>, Type>[] {
				EnabledBinder.Bind
			};
			var customBinders = new Action<Type, IEnumerable<FrameworkElement>, List<FrameworkElement>>[] {
				EnterBinder.Bind,
				SearchBinder.Bind
			};

			var defaultBindProperties = ViewModelBinder.BindProperties;
			var defaultBindActions = ViewModelBinder.BindActions;
			var defaultBind = ViewModelBinder.Bind;
			ViewModelBinder.Bind = (viewModel, view, context) => {
				defaultBind(viewModel, view, context);
				ContentElementBinder.Bind(viewModel, view, context);
				CommandBinder.BindCommand(view);
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
		}
	}
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Concurrency;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using ReactiveUI;
using LogManager = Caliburn.Micro.LogManager;

namespace AnalitF.Net.Client
{
	public class AppBootstrapper : Bootstrapper<ShellViewModel>
	{
		public static ShellViewModel Shell;

		public static Config.Initializers.NHibernate NHibernate;

		private string DataPath = "data";

		public AppBootstrapper()
		{
			var command = ApplicationCommands.Delete;
			LogManager.GetLog = t => new ConsoleLog();
			Tasks.Uri = new Uri("http://localhost:8080/Main/");
			Tasks.ArchiveFile = "archive.zip";
			Tasks.ExtractPath = ".";

			FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
				new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

			AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
				Console.WriteLine(args.ExceptionObject);
			};

			Application.DispatcherUnhandledException += (sender, args) => {
				args.Handled = true;
				Console.WriteLine(args.Exception);
			};

			RegisterBinder();

			NHibernate = new Config.Initializers.NHibernate();
			NHibernate.Init();
		}

		protected override void OnExit(object sender, EventArgs e)
		{
		}

		protected override void OnStartup(object sender, StartupEventArgs e)
		{
			new SanityCheck(DataPath).Check();

			base.OnStartup(sender, e);
		}

		protected override object GetInstance(Type service, string key)
		{
			if (typeof(IWindowManager) == service)
				return new Extentions.WindowManager();
			return base.GetInstance(service, key);
		}

		public static void RegisterBinder()
		{
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
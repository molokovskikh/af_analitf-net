using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.Tools;

namespace AnalitF.Net.Client
{
	public class AppBootstrapper : Bootstrapper<ShellViewModel>
	{
		public AppBootstrapper()
		{
			FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
				new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

			AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
				Console.WriteLine(args.ExceptionObject);
			};

			Application.DispatcherUnhandledException += (sender, args) => {
				args.Handled = true;
				Console.WriteLine(args.Exception);
			};
			LogManager.GetLog = t => new ConsoleLog();
			new Config.Initializers.NHibernate().Init();

			RegisterBinder();
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

			var defaultBindActions = ViewModelBinder.BindActions;
			var defaultBind = ViewModelBinder.Bind;
			ViewModelBinder.Bind = (viewModel, view, context) => {
				defaultBind(viewModel, view, context);
				ContentElementBinder.Bind(viewModel, view, context);
			};

			var customBinders = new Action<Type, IEnumerable<FrameworkElement>, List<FrameworkElement>>[] {
				EnterBinder.CustomBind,
				SearchBinder.CustomBind
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

	public class ConsoleLog : ILog
	{
		public void Info(string format, params object[] args)
		{
			Console.WriteLine(format, args);
		}

		public void Warn(string format, params object[] args)
		{
			Console.WriteLine(format, args);
		}

		public void Error(Exception exception)
		{
			Console.WriteLine(exception);
		}
	}
}
﻿using System;
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

		public AppBootstrapper()
		{
			var command = ApplicationCommands.Delete;
			LogManager.GetLog = t => new ConsoleLog();

			FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
				new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

			AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
				Console.WriteLine(args.ExceptionObject);
			};

			Application.DispatcherUnhandledException += (sender, args) => {
				args.Handled = true;
				Console.WriteLine(args.Exception);
			};

			new Config.Initializers.NHibernate().Init();
			RegisterBinder();
		}

		protected override void OnExit(object sender, EventArgs e)
		{
		}

		protected override void OnStartup(object sender, StartupEventArgs e)
		{
			new SanityCheck().Check();

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
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
			LogManager.GetLog = t => new ConsoleLog();
			new Config.Initializers.NHibernate().Init();

			RegisterBinder();
		}

		public static void RegisterBinder()
		{
			var defaultBindActions = ViewModelBinder.BindActions;
			var defaultBind = ViewModelBinder.Bind;
			ViewModelBinder.Bind = (viewModel, view, context) => {
				defaultBind(viewModel, view, context);
				ContentElementBinder.Bind(viewModel, view, context);
			};
			ViewModelBinder.BindActions = (elements, type) => {
				var binded = defaultBindActions(elements, type).ToList();

				foreach (var method in type.GetMethods()) {
					var name = method.Name;
					if (name.StartsWith("Enter")) {
						var element = elements.FindName(name.Replace("Enter", ""));
						if (element == null)
							continue;

						EnterBinder.Bind(method, element);
						binded.Add(element);
					}
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
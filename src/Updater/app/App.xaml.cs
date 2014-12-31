using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using log4net;
using log4net.Config;

namespace Updater
{
	public partial class App : Application
	{
		private static ILog log = LogManager.GetLogger(typeof(App));

		public App()
		{
			//проверка для ilmerge
			var merged = new [] {
				"log4net"
			};
			AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) => {
				if (merged.Any(n => eventArgs.Name.StartsWith(n)))
					return typeof(App).Assembly;
				return null;
			};

			XmlConfigurator.Configure();

			AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
				HandleException(args.ExceptionObject as Exception);
			};

			DispatcherUnhandledException += (sender, args) => {
				args.Handled = true;
				HandleException(args.Exception);
			};
		}

		public void HandleException(Exception exception)
		{
			NotifyAboutException(exception);
			Shutdown();
		}

		public static void NotifyAboutException(Exception exception)
		{
			log.Error("Ошибка в приложении", exception);
			MessageBox.Show("Не удалось обновить приложение. Пожалуйста обратитесь в АК Инфорум",
				"АналитФАРМАЦИЯ: Ошибка",
				MessageBoxButton.OK,
				MessageBoxImage.Error);
		}
	}
}

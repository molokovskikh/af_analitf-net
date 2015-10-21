using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
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
			//клиенты жалуются что при настройках по умолчанию текст "размыт"
			TextOptions.TextFormattingModeProperty.OverrideMetadata(typeof(Window),
				new FrameworkPropertyMetadata(TextFormattingMode.Display,
					FrameworkPropertyMetadataOptions.AffectsMeasure
						| FrameworkPropertyMetadataOptions.AffectsRender
						| FrameworkPropertyMetadataOptions.Inherits));

			//клиенты жалуются что шрифт слишком мелкий
			Control.FontSizeProperty.OverrideMetadata(typeof(TextBlock),
				new FrameworkPropertyMetadata(14d, FrameworkPropertyMetadataOptions.Inherits));
			Control.FontSizeProperty.OverrideMetadata(typeof(TextElement),
				new FrameworkPropertyMetadata(14d, FrameworkPropertyMetadataOptions.Inherits));

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
			MessageBox.Show("Не удалось обновить приложение. Пожалуйста обратитесь в АналитФармация",
				"АналитФАРМАЦИЯ: Ошибка",
				MessageBoxButton.OK,
				MessageBoxImage.Error);
		}
	}
}

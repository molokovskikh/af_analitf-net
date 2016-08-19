using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AnalitF.Net.Client.Views.Dialogs
{
	public partial class ScannerConfig : UserControl
	{
		public ViewModels.Dialogs.ScannerConfig Model => (ViewModels.Dialogs.ScannerConfig)DataContext;

		private DispatcherTimer timer = new DispatcherTimer {
			Interval = TimeSpan.FromMilliseconds(200)
		};

		public ScannerConfig()
		{
			InitializeComponent();
			Focusable = true;
			Loaded += (sender, args) => {
				Focus();
			};

			var buffer = new List<string>();
			timer.Start();
			timer.Tick += (sender, args) => {
				if (buffer.Count > 0) {
					Model.Input(buffer);
					buffer.Clear();
				}
			};
			PreviewKeyDown += (sender, args) => {
				timer.Stop();
				timer.Start();
				buffer.Add(KeyboardHook.KeyToUnicode(args.Key));
				args.Handled = true;
			};
		}
	}
}

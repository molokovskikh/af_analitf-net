using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AnalitF.Net.Client.Views.Dialogs
{
	public partial class ScannerConfig : UserControl
	{
		private KeyboardHook hook;

		public ViewModels.Dialogs.ScannerConfig Model => (ViewModels.Dialogs.ScannerConfig)DataContext;
		private DispatcherTimer timer = new DispatcherTimer {
			Interval = TimeSpan.FromMilliseconds(200)
		};

		public ScannerConfig()
		{
			InitializeComponent();

			var buffer = new List<string>();
			timer.Tick += (sender, args) => {
				if (buffer.Count > 0) {
					Model.Input(buffer);
					buffer.Clear();
				}
			};
			Loaded += (sender, args) => {
				timer.Start();
				hook = new KeyboardHook();
				hook.KeyboardInput += x => {
					buffer.Add(x);
					timer.Stop();
					timer.Start();
					return true;
				};
				hook.AddHook(Window.GetWindow(this));
			};

			Unloaded += (sender, args) => {
				timer.Stop();
				hook?.Dispose();
				hook = null;
			};
		}
	}
}

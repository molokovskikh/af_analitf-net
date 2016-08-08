using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AnalitF.Net.Client.Views.Dialogs
{
	/// <summary>
	/// Interaction logic for ScannerConfig.xaml
	/// </summary>
	public partial class ScannerConfig : UserControl
	{
		private KeyboardHook hook;

		public ScannerConfig()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				var subject = new Subject<string>();
				hook = new KeyboardHook();
				hook.KeyboardInput += x => {
					subject.OnNext(x);
					return true;
				};
				subject.Select(x => x[0] < 32 ? $"[{(int)x[0]}]"  : x)
					.Buffer(TimeSpan.FromMilliseconds(400), DispatcherScheduler.Current)
					.Where(x => x.Count > 0)
					.Subscribe(x => Code.Content = String.Concat(x));
				hook.AddHook(Window.GetWindow(this));
			};

			Unloaded += (sender, args) => {
				hook?.Dispose();
				hook = null;
			};
		}
	}
}

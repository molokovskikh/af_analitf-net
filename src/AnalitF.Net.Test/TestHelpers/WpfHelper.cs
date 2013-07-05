using System;
using System.Reactive.Concurrency;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using ReactiveUI;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class WpfHelper
	{
		public static void WithWindow(Action<Window> action)
		{
			var t = new Thread(() => {
				var window = new Window();
				action(window);
				window.Closed += (s, e) => window.Dispatcher.InvokeShutdown();
				window.Show();
				Dispatcher.Run();
			});

			t.SetApartmentState(ApartmentState.STA);
			t.Start();
			t.Join();
		}

		public static Dispatcher WithDispatcher(Action action)
		{
			var started = new ManualResetEventSlim();
			var dispatcherThread = new Thread(() => {
				Dispatcher.CurrentDispatcher.BeginInvoke(new Action(started.Set));
				Dispatcher.Run();
			});

			dispatcherThread.SetApartmentState(ApartmentState.STA);
			dispatcherThread.IsBackground = true;
			dispatcherThread.Start();
			started.Wait();
			var dispatcher = Dispatcher.FromThread(dispatcherThread);
			dispatcher.Invoke(new Action(() => {
				RxApp.DeferredScheduler = DispatcherScheduler.Current;
				action();
			}));
			return dispatcher;
		}
	}
}
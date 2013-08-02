using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Common.Tools.Calendar;
using NHibernate.Mapping;
using ReactiveUI;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class WpfHelper
	{
		public static void WithWindow(Action<Window> action)
		{
			var exceptions = new List<Exception>();
			var t = new Thread(() => {
				var window = new Window();
				try {
					window.Dispatcher.UnhandledException += (sender, args) => {
						exceptions.Add(args.Exception);
						window.Close();
					};
					action(window);
				}
				catch(Exception e) {
					exceptions.Add(e);
					throw;
				}
				window.Closed += (s, e) => window.Dispatcher.InvokeShutdown();
				window.Show();
			}) {
				IsBackground = true
			};

			t.SetApartmentState(ApartmentState.STA);
			t.Start();
			t.Join(20.Second());
			if (exceptions.Count > 0)
				throw new AggregateException(exceptions);
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
			dispatcher.Invoke(() => {
				RxApp.DeferredScheduler = DispatcherScheduler.Current;
				action();
			});
			return dispatcher;
		}
	}
}
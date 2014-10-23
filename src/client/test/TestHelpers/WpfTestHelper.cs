using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Common.Tools.Calendar;
using ReactiveUI;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public static class WpfTestHelper
	{
		public static void WithWindow(Action<Window> action)
		{
			var exceptions = new List<Exception>();
			var t = new Thread(() => {
				var window = new Window();
				SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(window.Dispatcher));
				try {
					window.Dispatcher.UnhandledException += (sender, args) => {
						args.Handled = true;
						exceptions.Add(args.Exception);
						window.Close();
						window.Dispatcher.InvokeShutdown();
					};
					action(window);
				}
				catch(Exception e) {
					exceptions.Add(e);
					throw;
				}
				window.Show();
				Dispatcher.Run();
			}) {
				IsBackground = true
			};

			t.SetApartmentState(ApartmentState.STA);
			t.Start();
			var stopped = t.Join(20.Second());
			if (!stopped)
				t.Abort();
			if (exceptions.Count > 0 && !(exceptions.FirstOrDefault() is TaskCanceledException))
				throw new AggregateException(exceptions);
			if (!stopped)
				throw new Exception("Тест не завершился добровольно убит по таймауту 20 секунд");
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

		public static TextCompositionEventArgs TextArgs(string text)
		{
			return new TextCompositionEventArgs(Keyboard.PrimaryDevice, new TextComposition(null, null, text)) {
				RoutedEvent = UIElement.TextInputEvent
			};
		}

		public static KeyEventArgs KeyEventArgs(DependencyObject o, Key key)
		{
			var keyEventArgs = new KeyEventArgs(Keyboard.PrimaryDevice,
				PresentationSource.CurrentSources.OfType<PresentationSource>().First(),
				0,
				key);
			keyEventArgs.RoutedEvent = UIElement.KeyDownEvent;
			return keyEventArgs;
		}

		//InvokeShutdown можно вызвать не всегда а только после того как окно загрузилось
		//если вызвать в onload то будет nre
		//что бы избежать этого делаем планируем запуск когда wpf сделает все свои дела
		public static void Shutdown(Window window)
		{
			window.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
				window.Close();
				window.Dispatcher.InvokeShutdown();
			}));
		}

		public static void SendKey(this FrameworkElement d, Key key)
		{
			d.RaiseEvent(KeyEventArgs(d, key));
		}

		public static DispatchAwaiter WaitIdle(this FrameworkElement d)
		{
			return d.Dispatcher.WaitIdle();
		}

		public static DispatchAwaiter WaitIdle(this Dispatcher d)
		{
			var src = new TaskCompletionSource<int>();
				d.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => src.SetResult(1)));
			return new DispatchAwaiter(src, d);
		}

		public static DispatchAwaiter WaitLoaded(this FrameworkElement element)
		{
			var src = new TaskCompletionSource<int>();
			if (element.IsLoaded)
				src.SetResult(1);
			else
				element.Loaded += (sender, args) => src.SetResult(1);
			return new DispatchAwaiter(src, element.Dispatcher);
		}
	}

	public class DispatchAwaiter : INotifyCompletion
	{
		public Dispatcher dispatcher;
		private TaskCompletionSource<int> src;

		public DispatchAwaiter(TaskCompletionSource<int> src, Dispatcher dispatcher)
		{
			this.src = src;
			this.dispatcher = dispatcher;
		}

		public void OnCompleted(Action continuation)
		{
			src.Task.ContinueWith(t => dispatcher.BeginInvoke(continuation));
		}

		public bool IsCompleted
		{
			get
			{
				return src.Task.IsCompleted;
			}
		}

		public void GetResult() { }

		public DispatchAwaiter GetAwaiter()
		{
			return this;
		}
	}
}
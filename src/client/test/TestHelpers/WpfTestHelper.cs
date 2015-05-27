using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Xml;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Test.Integration.Views;
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

		public static void WithWindow2(Func<Window, Task> action)
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
					action(window).ContinueWith(it => {
						if (it.IsFaulted)
							exceptions.AddRange(it.Exception.InnerExceptions);
						Shutdown(window);
					});
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

		public static KeyEventArgs KeyArgs(DependencyObject o, Key key)
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
			d.RaiseEvent(KeyArgs(d, key));
		}

		public static void SendText(this FrameworkElement d, string text)
		{
			d.RaiseEvent(TextArgs(text));
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

		public static void DumpXamlObj(ResourceDictionary obj)
		{
			var settings = new XmlWriterSettings { Indent = true };
			var writer = XmlWriter.Create(Console.Out, settings);
			XamlWriter.Save(obj, writer);
		}

		public static void CleanSafeError()
		{
			//на форме корректировки могут возникнуть ошибки биндинга
			//судя по обсуждению это ошибки wpf и они безобидны
			//http://wpf.codeplex.com/discussions/47047
			//игнорирую их
			var ignored = new[] {
				"System.Windows.Data Error: 4",
				"Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.DataGrid', AncestorLevel='1''. BindingExpression:Path=AreRowDetailsFrozen; DataItem=null; target element is 'DataGridDetailsPresenter' (Name=''); target property is 'SelectiveScrollingOrientation' (type 'SelectiveScrollingOrientation')",
				"Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.DataGrid', AncestorLevel='1''. BindingExpression:Path=HeadersVisibility; DataItem=null; target element is 'DataGridRowHeader' (Name=''); target property is 'Visibility' (type 'Visibility')",
				//todo - разобрать причину ошибки
				"Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.DataGrid', AncestorLevel='1''. BindingExpression:Path=NewItemMargin; DataItem=null; target element is 'DataGridRow' (Name=''); target property is 'Margin' (type 'Thickness')"
			};
			ViewSetup.BindingErrors.RemoveAll(s => ignored.Any(m => s.Contains(m)));
		}

		public static string FlowDocumentToText(FlowDocument doc)
		{
			var builder = new StringBuilder();
			foreach (var el in doc.Descendants().Distinct()) {
				if (el is Paragraph && !(((Paragraph)el).Parent is TableCell)) {
					builder.AppendLine();
				}
				if (el is Run) {
					builder.Append((((Run)el).Text ?? "").Trim());
				}
				if (el is TableRow) {
					builder.AppendLine();
				}
				if (el is TableCell) {
					builder.Append("|");
				}
			}
			return builder.ToString().Trim();
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
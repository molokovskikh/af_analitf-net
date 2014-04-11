using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.Tools;
using NUnit.Framework;
using ReactiveUI;
using Xceed.Wpf.Toolkit;
using Action = System.Action;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class DispatcherFixture : BaseViewFixture
	{
		protected Dispatcher dispatcher;
		protected Window activeWindow;
		protected List<Window> windows;
		protected List<Exception> exceptions;

		[SetUp]
		public void Setup()
		{
			exceptions = new List<Exception>();
			windows = new List<Window>();
			activeWindow = null;
			dispatcher = null;
			shell.Config.Quiet = true;
			shell.ViewModelSettings.Clear();

			manager.UnitTesting = false;
			manager.SkipApp = true;
			disposable.Add(manager.WindowOpened.Subscribe(w => {
				activeWindow = w;
				windows.Add(w);
				w.Closed += (sender, args) => {
					windows.Remove(w);
					activeWindow = windows.LastOrDefault();
				};
			}));

			disposable.Add(BindingChecker.Track());
			disposable.Add(Disposable.Create(() => {
				if (exceptions.Count > 0)
					throw new AggregateException(exceptions);
			}));
		}

		[TearDown]
		public void TearDown()
		{
			SystemTime.Reset();
			shell.Config.Quiet = false;
			if (dispatcher != null) {
				dispatcher.Invoke(() => {
					foreach (var window in windows.ToArray().Reverse())
						window.Close();
				});
				WaitIdle();
				dispatcher.Invoke(() => shell.Dispose());
				dispatcher.InvokeShutdown();
			}
		}

		public void DoubleClick(FrameworkElement view, UIElement element, object origin = null)
		{
			element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) {
				RoutedEvent = Control.MouseDoubleClickEvent,
				Source = origin,
			});
		}

		public void InternalClick(ButtonBase element)
		{
			Contract.Assert(element != null);
			AssertInputable(element);
			element.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, element));
		}

		public void InternalClick(Hyperlink element)
		{
			Contract.Assert(element != null);
			AssertInputable(element);
			element.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, element));
		}

		public void SimClikc(ButtonBase element)
		{
			element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) {
				RoutedEvent = Control.MouseDownEvent,
				Source = element,
			});
			element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) {
				RoutedEvent = Control.MouseUpEvent,
				Source = element,
			});
		}

		private void InternalClick(string name)
		{
			var el = activeWindow.FindName(name)
				?? activeWindow.Descendants<ButtonBase>().First(b => b.Name.Match(name));
			if (el is SplitButton)
				InternalClick(((SplitButton)el).Descendants<ButtonBase>().First());
			else
				InternalClick((ButtonBase)el);
		}

		protected void WaitIdle()
		{
			dispatcher.Invoke(() => {}, DispatcherPriority.ContextIdle);
		}

		public void Click(string name)
		{
			dispatcher.Invoke(() => InternalClick(name));
			WaitIdle();
		}

		protected void AsyncClick(string name)
		{
			dispatcher.BeginInvoke(new Action(() => InternalClick(name)));
			WaitIdle();
		}

		protected void AsyncClickNoWait(string name)
		{
			dispatcher.BeginInvoke(new Action(() => InternalClick(name)));
		}

		protected void Input(FrameworkElement view, string name, string text)
		{
			view.Dispatcher.Invoke(() => {
				Input((UIElement)view.FindName(name), text);
			});
		}

		protected void Input(UIElement element, string text)
		{
			Contract.Assert(element != null);
			AssertInputable(element);
			element.RaiseEvent(WpfHelper.TextArgs(text));
		}

		protected static void Input(UIElement element, Key key)
		{
			Contract.Assert(element != null);
			AssertInputable(element);
			element.RaiseEvent(WpfHelper.KeyEventArgs(element, key));
		}

		protected void Input(string name, Key key)
		{
			var item = (BaseScreen)shell.ActiveItem;
			var view = item.GetView();
			Contract.Assert(view != null);
			Input((FrameworkElement)view, name, key);
		}

		protected void Input(string name, string text)
		{
			var item = (BaseScreen)shell.ActiveItem;
			var view = item.GetView();
			Contract.Assert(view != null);
			Input((FrameworkElement)view, name, text);
		}

		protected void Input(FrameworkElement view, string name, Key key)
		{
			view.Dispatcher.Invoke(() => {
				var element = (UIElement)view.FindName(name);
				if (element == null)
					throw new Exception(String.Format("Не могу найти {0}", name));
				Input(element, key);
			});
		}

		protected static void AssertInputable(UIElement element)
		{
			Assert.IsTrue(element.IsVisible, element.ToString());
			Assert.IsTrue(element.IsEnabled, element.ToString());
		}
		protected static void AssertInputable(ContentElement element)
		{
			Assert.IsTrue(element.IsEnabled, element.ToString());
		}

		protected async void Start()
		{
			session.Flush();

			var loaded = new SemaphoreSlim(0, 1);

			dispatcher = WpfHelper.WithDispatcher(() => {
				//wpf обеспечивает синхронизациию объектов ui
				//тк сам тест запускает в отдельной нитке то в статических полях StyleHelper могут содержаться объекты созданные
				//в других нитках что бы избежать ошибок очищаем статические структуры
				StyleHelper.Reset();
				//DeferredScheduler TaskpoolScheduler thread static тк dispatcher выполняется в своей нитке
				//здесь будет непойми что
				var originDeferred = RxApp.DeferredScheduler;
				var originTask = RxApp.TaskpoolScheduler;
				var originbus = RxApp.MessageBus;
				RxApp.DeferredScheduler = testScheduler;
				RxApp.TaskpoolScheduler = testScheduler;
				RxApp.MessageBus = bus;
				disposable.Add(Disposable.Create(() => {
					RxApp.DeferredScheduler = originDeferred;
					RxApp.TaskpoolScheduler = originTask;
					RxApp.MessageBus = originbus;
				}));
				activeWindow = (Window)ViewLocator.LocateForModel(shell, null, null);
				windows.Add(activeWindow);
				activeWindow.Loaded += (sender, args) => loaded.Release();
				ViewModelBinder.Bind(shell, activeWindow, null);
				activeWindow.Show();
			});
			dispatcher.UnhandledException += (sender, args) => {
				args.Handled = true;
				exceptions.Add(args.Exception);
			};

			await loaded.WaitAsync();
			WaitIdle();
		}
	}
}
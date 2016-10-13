using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Test.Unit;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.Tools;
using Microsoft.Reactive.Testing;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using ReactiveUI;
using Xceed.Wpf.Toolkit;
using Action = System.Action;
using Hyperlink = System.Windows.Documents.Hyperlink;
using System.Reactive.Linq;
using Common.Tools.Calendar;
using System.Windows.Automation;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	[Apartment(ApartmentState.STA)]
	public class DispatcherFixture : BaseViewFixture
	{
		protected Dispatcher dispatcher;
		protected Window activeWindow;
		protected UserControl activeTab;
		protected List<Window> windows;
		protected List<Exception> exceptions;

		[SetUp]
		public void Setup()
		{
			ConventionManager.DefaultHeaderTemplate = null;
			ConventionManager.DefaultItemTemplate = null;
			exceptions = new List<Exception>();
			windows = new List<Window>();
			activeWindow = null;
			dispatcher = null;
			config.Quiet = true;

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
				if (DbHelper.IsTestFail() && IsCI()
					&& activeWindow != null) {
					dispatcher.Invoke(() => {
						PrintFixture.SaveToPng(activeWindow, DbHelper.FailDir("screen.png"));
					});
				}
				dispatcher.Invoke(() => {
					foreach (var window in windows.ToArray().Reverse())
						window.Close();
				});
				WaitIdle();
				dispatcher.Invoke(() => shell.Dispose());
				dispatcher.InvokeShutdown();
			}
		}

		public static bool IsCI()
		{
			return !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILD_NUMBER"));
		}

		public void DoubleClick(UIElement element, object origin = null)
		{
			AssertInputable(element);
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

		public void SimClick(ButtonBase element)
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
				?? activeWindow.Descendants<ButtonBase>().FirstOrDefault(b => b.Name.Match(name));
			if (el == null)
				throw new Exception($"Не могу найти кнопку '{name}'");
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

		protected void AsyncClickSplit(string name, string header)
		{
			dispatcher.BeginInvoke(new Action(() =>
			{
				var el = activeWindow.FindName(name) ?? activeWindow.Descendants<SplitButton>().First(x => x.Name.Match(name));
				if (el == null)
					throw new Exception($"Не могу найти пункт меню с заголовком '{header}' в окне {activeWindow}");
				var button = el as SplitButton;
				AssertInputable(button);
				var menuItem = (button.DropDownContent as ItemsControl).Descendants<MenuItem>().First(x => x.Header.ToString().Match(header));
				button.IsOpen = true;
				menuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, menuItem));
			}));
			WaitIdle();
		}

		protected void AsyncClickNoWait(string name)
		{
			dispatcher.BeginInvoke(new Action(() => InternalClick(name)));
		}

		protected void Input(FrameworkElement view, string name, string text)
		{
			view.Dispatcher.Invoke(() => {
				var el = (UIElement)view.FindName(name);
				Assert.IsNotNull(el, $"Элемент {name} не найден");
				Input(el, text);
			});
		}

		protected void Input(UIElement element, string text)
		{
			Assert.IsNotNull(element);
			AssertInputable(element);
			element.RaiseEvent(WpfTestHelper.TextArgs(text));
		}

		public void InputActiveWindow(string name, string text)
		{
			dispatcher.Invoke(() => {
				var el = activeWindow.Descendants<FrameworkElement>().FirstOrDefault(e => e.Name == name);
				if (el == null)
					throw new Exception($"Могу найти элемент с именем '{name}' в окне {activeWindow}");
				AssertInputable(el);
				el.RaiseEvent(WpfTestHelper.TextArgs(text));
			});
		}

		protected static void Input(UIElement element, Key key)
		{
			Contract.Assert(element != null);
			AssertInputable(element);
			element.RaiseEvent(WpfTestHelper.KeyArgs(element, key));
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
					throw new Exception($"Не могу найти {name}");
				Input(element, key);
			});
		}

		protected static void AssertInputable(UIElement element)
		{
			Assert.IsTrue(element.IsVisible, "элемент {0} {1} невидим IsVisible=False", element, ((FrameworkElement)element).Name);
			Assert.IsTrue(element.IsEnabled, "элемент {0} {1} недоступен для ввода IsEnabled=False", element, ((FrameworkElement)element).Name);
		}

		protected static void AssertInputable(ContentElement element)
		{
			Assert.IsTrue(element.IsEnabled, element.ToString());
		}

		public void StartWait()
		{
			Start().Wait();
		}

		protected async Task Start()
		{
			session.Flush();

			var loaded = new SemaphoreSlim(0, 1);

			dispatcher = WpfTestHelper.WithDispatcher(() => {
				//wpf обеспечивает синхронизацию объектов ui
				//тк сам тест запускает в отдельной нитке то в статических полях StyleHelper могут содержаться объекты созданные
				//в других нитках что бы избежать ошибок очищаем статические структуры
				StyleHelper.Reset();
				activeWindow = (Window)ViewLocator.LocateForModel(shell, null, null);
				//такой размер нужен что бы уместились все кнопки на панели инструментов
				//тк невидимую кнопку нельзя нажать
				activeWindow.Width = 1014;
				activeWindow.Height = 764;
				windows.Add(activeWindow);
				activeWindow.Loaded += (sender, args) => loaded.Release();
				ViewModelBinder.Bind(shell, activeWindow, null);
				//что бы тесты не лезли на первый план
				activeWindow.ShowActivated = false;
				activeWindow.ShowInTaskbar = false;
				activeWindow.Show();
			});
			Env.UiScheduler = new MixedScheduler((TestScheduler)Env.UiScheduler, new DispatcherScheduler(dispatcher));

			dispatcher.UnhandledException += (sender, args) => {
				args.Handled = true;
				//ошибки отмены могут возникнуть если мы закроем форму до завершения всех запросов
				//игнорируем их
				if (!(args.Exception is TaskCanceledException))
					exceptions.Add(args.Exception);
			};

			await loaded.WaitAsync();
			WaitIdle();
		}

		public T ByName<T>(DependencyObject root, string name) where T : FrameworkElement
		{
			return root.Descendants<T>().First(c => c.Name == name);
		}

		public T ByName<T>(string name) where T : FrameworkElement
		{
			return ByName<T>(activeTab, name);
		}

		protected void WaitWindow(string title, string body = null)
		{
			var found = false;
			dispatcher.Invoke(() => {
				found = activeWindow.Title == title;
			});
			if (found)
				return;
			var opened = manager.WindowOpened.Timeout(30.Second()).First();
			opened.Dispatcher.Invoke(() => {
				var text = opened.AsText();
				Assert.AreEqual(title, opened.Title, text);
				if (!String.IsNullOrEmpty(body))
					Assert.That(text, Does.Contain(body), text);
			});
		}
	}
}
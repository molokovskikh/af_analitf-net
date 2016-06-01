using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.Tools.Calendar;
using Common.Tools.Helpers;
using NUnit.Framework;
using TestStack.White.InputDevices;

namespace AnalitF.Net.Client.Test.Acceptance
{
	public class BaseFixture
	{
		protected string lastId;
		protected bool IsDebug;
		protected string Bin;

		protected TimeSpan Timeout;
		protected TimeSpan UpdateTimeout;

		protected Process Process;

		protected AutomationElement MainWindow;
		protected StreamWriter Writer;

		protected Action<AutomationElement> DialogHandle;
		protected Action<AutomationElement> WindowHandle;

		protected Subject<AutomationElement> Opened;
		protected bool FilterByProcess;

		[SetUp]
		public void BaseFixtureSetup()
		{
			FilterByProcess = true;
			Writer = null;
			lastId = null;
			IsDebug = true;
			Process = null;
			Opened = new Subject<AutomationElement>();

			WindowHandle = w => {};
			DialogHandle = w => {};

			Timeout = 5.Second();
			UpdateTimeout = 50.Second();
			Bin = Path.Combine("acceptance", "AnalitF.Net.Client.exe");

			Automation.AddAutomationEventHandler(
				WindowPatternIdentifiers.WindowOpenedEvent,
				AutomationElement.RootElement,
				TreeScope.Subtree,
				OnActivated);
		}

		[TearDown]
		public void BaseFixtureTeardown()
		{
			Opened.OnCompleted();
			Opened.Dispose();

			MainWindow = null;
			Close(Process);
			Automation.RemoveAllEventHandlers();
		}

		protected void Close(Process process)
		{
			if (process == null)
				return;
			if (process.HasExited)
				return;
			process.CloseMainWindow();
			process.WaitForExit(TimeSpan.FromSeconds(10).Milliseconds);
			if (!process.HasExited) {
				process.Kill();
				WaitHelper.Wait(40.Second(), () => {
					try {
						Process.GetProcessById(process.Id);
						return false;
					}
					catch (ArgumentException) {
						return true;
					}
				});
			}
		}

		protected AutomationElement FindByName(string name, AutomationElement element = null)
		{
			element = element ?? MainWindow;
			return element.FindAll(
				TreeScope.Descendants,
				new PropertyCondition(
					AutomationElement.NameProperty,
					name,
					PropertyConditionFlags.IgnoreCase))
				.Cast<AutomationElement>()
				.FirstOrDefault();
		}

		protected AutomationElement FindById(string name, AutomationElement element = null)
		{
			element = element ?? MainWindow;
			return element.FindAll(
				TreeScope.Descendants,
				new PropertyCondition(
					AutomationElement.AutomationIdProperty,
					name,
					PropertyConditionFlags.IgnoreCase))
				.Cast<AutomationElement>()
				.FirstOrDefault();
		}

		protected Process StartProcess(string fileName, string arguments = "")
		{
			var process = new Process();
			process.StartInfo.FileName = fileName;
			process.StartInfo.Arguments = arguments;
			process.Start();
			process.EnableRaisingEvents = true;
			return process;
		}

		protected void ClickByName(string name, AutomationElement element = null)
		{
			var launchButton = FindByName(name, element);
			if (launchButton == null)
				throw new Exception(String.Format("Не могу найти кнопку {0}", name));

			launchButton.Invoke();
		}

		protected void Click(string id, AutomationElement element = null)
		{
			var launchButton = FindById(id, element);
			if (launchButton == null)
				throw new Exception(String.Format("Не могу найти кнопку {0}, форма {1}", id, AutomationHelper.ToText(element)));

			launchButton.Invoke();
		}

		protected virtual void OnActivated(object sender, AutomationEventArgs e)
		{
			var el = (AutomationElement)sender;
			if (FilterByProcess
				&& !Process.HasExited
				&& el.Current.ProcessId != Process.Id)
				return;

			var currentId = el.ToShortText();
			if (lastId == currentId)
				return;
			lastId = currentId;

			Opened.OnNext(el);

			WindowHandle(el);
			if (MainWindow == null) {
				var id = (string)el.GetCurrentPropertyValue(AutomationElement.AutomationIdProperty);
				if (id == "Shell") {
					MainWindow = el;
				}
			}
			else if (DialogHandle != null
				&& e.EventId.ProgrammaticName == WindowPatternIdentifiers.WindowOpenedEvent.ProgrammaticName) {
				DialogHandle(el);
			}
		}

		protected AutomationElementCollection FindTextElements(string text)
		{
			return MainWindow.FindAll(TreeScope.Subtree,
				new AndCondition(
					new PropertyCondition(AutomationElement.NameProperty, text, PropertyConditionFlags.IgnoreCase),
					new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text)));
		}

		public void Wait(Func<bool> func, string message = null)
		{
			WaitHelper.WaitOrFail(Timeout, () => !func(), message);
		}

		public void WaitWindow(string caption)
		{
			Wait(() => WinApi.FindWindow(IntPtr.Zero, "АналитФАРМАЦИЯ") == IntPtr.Zero);
			MainWindow = AutomationElement.FromHandle(WinApi.FindWindow(IntPtr.Zero, "АналитФАРМАЦИЯ"));
		}

		protected void WaitText(string text)
		{
			Wait(() => FindTextElements(text).Count == 0);
		}

		protected AutomationElement WaitForElement(string name)
		{
			Wait(() => FindById(name, MainWindow) == null, String.Format("появления элемента {0}", name));
			return FindById(name, MainWindow);
		}

		protected virtual void Activate()
		{
			var debugPipe = Guid.NewGuid().ToString();
			var pipe = new NamedPipeServerStream(debugPipe, PipeDirection.InOut);
			Writer = new StreamWriter(pipe);

			var arguments = "";
			if (IsDebug)
				arguments = "--debug-pipe=" + debugPipe;
			Process = StartProcess(Bin, arguments);
			WaitMainWindow();
			if (IsDebug) {
				pipe.WaitForConnection();
				Writer.AutoFlush = true;
			}

			WaitIdle();
		}

		protected static void DoubleClickCell(AutomationElement table, int row, int column)
		{
			var cell = ((GridPattern)table.GetCurrentPattern(GridPattern.Pattern)).GetItem(row, column);
			var rect = (Rect)cell.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
			Mouse.Instance.DoubleClick(new Point(rect.X + 3, rect.Y + 3));
		}

		protected static void ClickCell(AutomationElement table, int row, int column)
		{
			var cell = ((GridPattern)table.GetCurrentPattern(GridPattern.Pattern)).GetItem(row, column);
			var rect = (Rect)cell.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
			Mouse.Instance.Click(new Point(rect.X + 3, rect.Y + 3));
		}

		protected static void RightClick(AutomationElement element)
		{
			var rect = (Rect)element.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
			Mouse.Instance.RightClick(new Point(rect.X + 10, rect.Y + 10));
		}

		protected void WaitMainWindow()
		{
			Wait(() => MainWindow == null);
		}

		protected void HandleDialogs()
		{
			DialogHandle = e => {
				//отказываемся от всего
				ClickByName("Нет", e);
			};
		}

		protected WindowPattern WaitIdle()
		{
			var close = (WindowPattern)MainWindow.GetCurrentPattern(WindowPattern.Pattern);
			close.WaitForInputIdle((int)10.Second().TotalMilliseconds);
			return close;
		}

		protected void Type(string el, string text)
		{
			var username = WaitForElement(el);
			username.SetFocus();
			Keyboard.Instance.Enter(text);
		}

		protected void WaitMessage(string message, string button = "ОК")
		{
			var window = WaitDialog("АналитФАРМАЦИЯ: Внимание");
			Assert.AreEqual(message, AutomationHelper.ToText(window));
			ClickByName(button, window);
		}

		protected void AssertUpdate(string result)
		{
			var update = WaitDialog("Обмен данными");
			Assert.AreEqual("Обмен данными", update.GetName(), AutomationHelper.ToText(update));
			var dialog = Opened.Timeout(UpdateTimeout).First();
			if (dialog.GetName() == "Обмен данными") {
				dialog = Opened.Timeout(UpdateTimeout).First();
			}

			//может быть простое уведомление а может быть уведомление о новых документах
			var text = AutomationHelper.ToText(dialog);
			if (text == result) {
				Assert.AreEqual(result, text);
				ClickByName("Закрыть", dialog);
			}
			else {
				Assert.That(text, Does.Contain(result));
				ClickByName("TryClose", dialog);
			}
		}

		protected AutomationElement WaitDialog(string name, TimeSpan timeout)
		{
			var observable = Opened.Take(1).PublishLast();
			using (observable.Connect())
				return AutomationHelper.FindWindow(name, Process.Id) ?? observable.Timeout(timeout).First();
		}

		protected AutomationElement WaitDialog(string name)
		{
			return WaitDialog(name, Timeout);
		}

		protected void AssertText(AutomationElement el, string text)
		{
			Assert.That(AutomationHelper.ToText(el), Does.Contain(text), el.ToShortText());
		}
	}
}
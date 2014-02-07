using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.Tools;
using Common.Tools.Calendar;
using Microsoft.Test.Input;
using NUnit.Framework;
using Condition = System.Windows.Automation.Condition;

namespace AnalitF.Net.Client.Test.Acceptance
{
	public class BaseFixture
	{
		protected string root;
		protected string exe;

		protected TimeSpan Timeout;

		protected Process Process;

		protected AutomationElement MainWindow;
		protected StreamWriter Writer;

		protected Action<AutomationElement> DialogHandle = w => {};
		protected Action<AutomationElement> WindowHandle = w => {};

		protected Subject<AutomationElement> Opened;
		private string lastId;

		[SetUp]
		public void Setup()
		{
			Opened = new Subject<AutomationElement>();

			WindowHandle = w => {};
			DialogHandle = w => {};

			Timeout = TimeSpan.FromSeconds(5);
			root = @"acceptance";
			exe = Path.Combine(root, "AnalitF.Net.Client.exe");

			Automation.AddAutomationEventHandler(
				WindowPatternIdentifiers.WindowOpenedEvent,
				AutomationElement.RootElement,
				TreeScope.Subtree,
				OnActivated);
		}

		[TearDown]
		public void Teardown()
		{
			Opened.OnCompleted();
			Opened.Dispose();

			MainWindow = null;
			if (Process != null) {
				if (!Process.HasExited)
					Process.CloseMainWindow();

				Process.WaitForExit(TimeSpan.FromSeconds(10).Milliseconds);
				if (!Process.HasExited) {
					Process.Kill();
					SpinWait.SpinUntil(() => {
						try {
							Process.GetProcessById(Process.Id);
							return false;
						}
						catch(ArgumentException) {
							return true;
						}
					});
				}
			}
			Automation.RemoveAllEventHandlers();
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

		protected Process StartProcess(string fileName, string dir = "", string arguments = "")
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

			var invokePattern = (InvokePattern)launchButton.GetCurrentPattern(InvokePattern.Pattern);
			invokePattern.Invoke();
		}

		protected void Click(string id, AutomationElement element = null)
		{
			var launchButton = FindById(id, element);
			if (launchButton == null)
				throw new Exception(String.Format("Не могу найти кнопку {0}", id));

			var invokePattern = (InvokePattern)launchButton.GetCurrentPattern(InvokePattern.Pattern);
			invokePattern.Invoke();
		}

		private void OnActivated(object sender, AutomationEventArgs e)
		{
			var currentId = ((AutomationElement)sender).ToShortText();
			if (lastId == currentId)
				return;
			lastId = currentId;

			Opened.OnNext(sender as AutomationElement);

			var el = sender as AutomationElement;
			WindowHandle(el);
			if (MainWindow == null) {
				var id = (string)el.GetCurrentPropertyValue(AutomationElement.AutomationIdProperty);
				if (id == "Shell") {
					MainWindow = el;
				}
			}
			else if (DialogHandle != null && e.EventId.ProgrammaticName == WindowPatternIdentifiers.WindowOpenedEvent.ProgrammaticName) {
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

		protected void Wait(Func<bool> func, string message = null)
		{
			var elapsed = new TimeSpan();
			var wait = TimeSpan.FromMilliseconds(100);
			while (func()) {
				Thread.Sleep(wait);
				elapsed += wait;
				if (elapsed > Timeout)
					throw new Exception(String.Format("Не удалось дождаться за {0} {1}", Timeout, message));
			}
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

		protected void Activate()
		{
			var debugPipe = Guid.NewGuid().ToString();
			var pipe = new NamedPipeServerStream(debugPipe, PipeDirection.InOut);
			Writer = new StreamWriter(pipe);

			Process = StartProcess(exe, root, "--debug-pipe=" + debugPipe);
			WaitMainWindow();
			pipe.WaitForConnection();
			Writer.AutoFlush = true;

			WaitIdle();
		}

		protected static void DoubleClickCell(AutomationElement table, int row, int column)
		{
			var cell = ((GridPattern)table.GetCurrentPattern(GridPattern.Pattern)).GetItem(row, column);
			var rect = (Rect)cell.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
			Mouse.MoveTo(new System.Drawing.Point((int)rect.X + 3, (int)rect.Y + 3));
			Mouse.DoubleClick(MouseButton.Left);
		}

		protected static void ClickCell(AutomationElement table, int row, int column)
		{
			var cell = ((GridPattern)table.GetCurrentPattern(GridPattern.Pattern)).GetItem(row, column);
			var rect = (Rect)cell.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
			Mouse.MoveTo(new System.Drawing.Point((int)rect.X + 3, (int)rect.Y + 3));
			Mouse.Click(MouseButton.Left);
		}

		protected static void RightClick(AutomationElement element)
		{
			var rect = (Rect)element.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
			Mouse.MoveTo(new System.Drawing.Point((int)rect.X + 10, (int)rect.Y + 10));
			Mouse.DoubleClick(MouseButton.Right);
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
	}
}
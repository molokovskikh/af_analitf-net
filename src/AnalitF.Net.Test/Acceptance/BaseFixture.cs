using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using Common.Tools;
using Microsoft.Test.Input;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Acceptance
{
	public class BaseFixture
	{
		[DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
		public static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

		protected TimeSpan Timeout = TimeSpan.FromSeconds(5);

		protected Process Process;

		protected AutomationElement MainWindow;
		protected string Exe;
		protected StreamWriter Writer;

		protected Action<AutomationElement> DialogHandler;

		[SetUp]
		public void Setup()
		{
			Exe = @"..\..\..\AnalitF.Net.Client\bin\Debug";
		}

		[TearDown]
		public void Teardown()
		{
			MainWindow = null;
			if (Process != null) {
				if (!Process.HasExited)
					Process.CloseMainWindow();

				Process.WaitForExit(TimeSpan.FromSeconds(10).Milliseconds);
				if (!Process.HasExited)
					Process.Kill();
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

		protected void StartProcess(string fileName, string dir = "", string arguments = "")
		{
			Process = new Process();
			Process.StartInfo.FileName = fileName;
			Process.StartInfo.Arguments = arguments;
			//Process.StartInfo.WorkingDirectory = dir;
			Process.Start();
			Process.EnableRaisingEvents = true;

			Automation.AddAutomationEventHandler(
				WindowPatternIdentifiers.WindowOpenedEvent,
				AutomationElement.RootElement,
				TreeScope.Subtree,
				OnActivated);

			Wait(() => MainWindow == null);
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
			if (MainWindow == null) {
				MainWindow = AutomationElement.FromHandle(Process.MainWindowHandle);
			}
			else if (DialogHandler != null && e.EventId.ProgrammaticName == WindowPatternIdentifiers.WindowOpenedEvent.ProgrammaticName) {
				DialogHandler(sender as AutomationElement);
			}
		}

		public static void Dump(AutomationElementCollection elements)
		{
			foreach (var element in elements.Cast<AutomationElement>()) {
				Dump(element);
			}
		}

		private static void Dump(AutomationElement element)
		{
			if (element == null)
				return;

			Console.WriteLine("--------------");
			foreach (var p in element.GetSupportedProperties()) {
				Console.WriteLine("{0} = {1}", p.ProgrammaticName, element.GetCurrentPropertyValue(p));
			}

			foreach (var pattern in element.GetSupportedPatterns()) {
				Console.WriteLine(pattern.ProgrammaticName);
			}
		}

		protected AutomationElementCollection FindTextElements(string text)
		{
			return MainWindow.FindAll(TreeScope.Subtree,
				new AndCondition(
					new PropertyCondition(AutomationElement.NameProperty, text, PropertyConditionFlags.IgnoreCase),
					new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text)));
		}

		protected AutomationElementCollection FindTextElements(AutomationElement element)
		{
			return element.FindAll(TreeScope.Subtree,
				new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text));
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
			Wait(() => FindWindowByCaption(IntPtr.Zero, "АналитФАРМАЦИЯ") == IntPtr.Zero);
			MainWindow = AutomationElement.FromHandle(FindWindowByCaption(IntPtr.Zero, "АналитФАРМАЦИЯ"));
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
			var root = "acceptance";
			Prepare(Exe, root);

			var debugPipe = Guid.NewGuid().ToString();
			var pipe = new NamedPipeServerStream(debugPipe, PipeDirection.InOut);
			Writer = new StreamWriter(pipe);

			StartProcess(Path.Combine(root, "AnalitF.Net.Client.exe"), root, "--debug-pipe=" + debugPipe);
			pipe.WaitForConnection();
			Writer.AutoFlush = true;
		}

		protected static void CopyDir(string src, string dst)
		{
			if (!Directory.Exists(dst)) {
				Directory.CreateDirectory(dst);
			}

			foreach (var file in Directory.GetFiles(src)) {
				File.Copy(file, Path.Combine(dst, Path.GetFileName(file)), true);
			}

			foreach (var dir in Directory.GetDirectories(src)) {
				CopyDir(dir, Path.Combine(dst, Path.GetFileName(dir)));
			}
		}

		protected static void Prepare(string exe, string root)
		{
			if (!Directory.Exists(root))
				Directory.CreateDirectory(root);

			var files = Directory.GetFiles(exe, "*.exe")
				.Concat(Directory.GetFiles(exe, "*.dll"))
				.Concat(Directory.GetFiles(exe, "*.config"));
			files.Each(f => File.Copy(f, Path.Combine(root, Path.GetFileName(f)), true));

			CopyDir("share", Path.Combine(root, "share"));
			CopyDir("backup", Path.Combine(root, "data"));
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
	}
}
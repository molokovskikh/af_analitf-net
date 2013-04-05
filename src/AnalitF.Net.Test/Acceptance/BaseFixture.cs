using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Automation;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Acceptance
{
	public class BaseFixture
	{
		[DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
		public static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

		protected TimeSpan timeout = TimeSpan.FromSeconds(5);

		protected Process process;

		protected AutomationElement MainWindow;

		[TearDown]
		public void Teardown()
		{
			MainWindow = null;
			if (process != null) {
				if (!process.HasExited)
					process.CloseMainWindow();

				process.WaitForExit(TimeSpan.FromSeconds(10).Milliseconds);
				if (!process.HasExited)
					process.Kill();
			}
		}

		protected AutomationElement FindByName(string name)
		{
			return MainWindow.FindAll(
				TreeScope.Descendants,
				new PropertyCondition(
					AutomationElement.NameProperty,
					name,
					PropertyConditionFlags.IgnoreCase))
				.Cast<AutomationElement>()
				.FirstOrDefault();
		}

		protected AutomationElement FindById(string name)
		{
			return MainWindow.FindAll(
				TreeScope.Descendants,
				new PropertyCondition(
					AutomationElement.AutomationIdProperty,
					name,
					PropertyConditionFlags.IgnoreCase))
				.Cast<AutomationElement>()
				.FirstOrDefault();
		}

		protected void Wait(Func<bool> func)
		{
			var elapsed = new TimeSpan();
			var wait = TimeSpan.FromMilliseconds(100);
			while (func()) {
				Thread.Sleep(wait);
				elapsed += wait;
				if (elapsed > timeout)
					throw new Exception("Не удалось дождаться");
			}
		}

		protected void StartProcess(string fileName, string arguments = "")
		{
			process = new Process();
			process.StartInfo.FileName = fileName;
			process.StartInfo.Arguments = arguments;
			process.Start();
			process.EnableRaisingEvents = true;

			Automation.AddAutomationEventHandler(
				WindowPatternIdentifiers.WindowOpenedEvent,
				AutomationElement.RootElement,
				TreeScope.Subtree,
				OnActivated);

			Wait(() => MainWindow == null);
		}

		protected void ClickByName(string name)
		{
			var launchButton = FindByName(name);
			if (launchButton == null)
				throw new Exception(String.Format("Не могу найти кнопку {0}", name));

			var invokePattern = (InvokePattern)launchButton.GetCurrentPattern(InvokePattern.Pattern);
			invokePattern.Invoke();
		}

		protected void Click(string id)
		{
			var launchButton = FindById(id);
			if (launchButton == null)
				throw new Exception(String.Format("Не могу найти кнопку {0}", id));

			var invokePattern = (InvokePattern)launchButton.GetCurrentPattern(InvokePattern.Pattern);
			invokePattern.Invoke();
		}

		private void OnActivated(object sender, AutomationEventArgs e)
		{
			MainWindow = AutomationElement.FromHandle(process.MainWindowHandle);
		}

		public static void Dump(AutomationElementCollection elements)
		{
			foreach (var element in elements.Cast<AutomationElement>()) {
				Console.WriteLine("--------------");
				foreach (AutomationProperty p in element.GetSupportedProperties()) {
					Console.WriteLine("{0} = {1}", p.ProgrammaticName, element.GetCurrentPropertyValue(p));
				}
			}
		}

		public void WaitWindow(string caption)
		{
			Wait(() => FindWindowByCaption(IntPtr.Zero, "АналитФАРМАЦИЯ") == IntPtr.Zero);
			MainWindow = AutomationElement.FromHandle(FindWindowByCaption(IntPtr.Zero, "АналитФАРМАЦИЯ"));
		}

		protected AutomationElementCollection FindTextElements(string text)
		{
			return MainWindow.FindAll(TreeScope.Subtree,
				new AndCondition(
					new PropertyCondition(AutomationElement.NameProperty, text, PropertyConditionFlags.IgnoreCase),
					new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text)));
		}

		protected void WaitText(string text)
		{
			Wait(() => FindTextElements(text).Count == 0);
		}
	}
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Automation;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Tools.Helpers;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public static class AutomationHelper
	{
		public static void Invoke(this AutomationElement el)
		{
			var invokePattern = (InvokePattern)el.GetCurrentPattern(InvokePattern.Pattern);
			invokePattern.Invoke();
		}

		public static void SetValue(this AutomationElement el, string value)
		{
			var invokePattern = (ValuePattern)el.GetCurrentPattern(ValuePattern.Pattern);
			invokePattern.SetValue(value);
		}

		public static void TraceWindow(object sender, AutomationEventArgs e)
		{
			Console.WriteLine("{0:ss.fff} {1} {2}",
				DateTime.Now,
				((AutomationElement)sender).ToShortText(),
				e.EventId.ProgrammaticName);
			Console.WriteLine("-----------------------------");
			Console.WriteLine(ToText(sender as AutomationElement));
			Console.WriteLine("-----------------------------");
		}

		public static string GetName(this AutomationElement e)
		{
			return (string)e.GetCurrentPropertyValue(AutomationElement.NameProperty);
		}

		public static int GetProcessId(this AutomationElement e)
		{
			return (int)e.GetCurrentPropertyValue(AutomationElement.ProcessIdProperty);
		}

		public static void Dump(AutomationElementCollection elements)
		{
			Dump((AutomationElementCollection)elements.Cast<AutomationElement>());
		}

		public static string ToShortText(this AutomationElement el)
		{
			return String.Format("{0} - {1}", el.GetRuntimeId().Implode(), el.GetName());
		}

		public static void Dump(AutomationElement element)
		{
			if (element == null)
				return;

			Console.WriteLine("--------");
			Console.WriteLine("{0} {1}", element, element.GetHashCode());
			Console.WriteLine("--props--");
			foreach (var p in element.GetSupportedProperties()) {
				var value = element.GetCurrentPropertyValue(p);
				Console.WriteLine("{0} = {1} ({2})", p.ProgrammaticName,
					value is AutomationIdentifier ? ((AutomationIdentifier)value).ProgrammaticName : value,
					value != null ? value.GetType().ToString() : "");
			}

			Console.WriteLine("--patterns--");
			foreach (var pattern in element.GetSupportedPatterns()) {
				Console.WriteLine(pattern.ProgrammaticName);
			}
			Console.WriteLine("--------");
		}

		public static void Dump(IEnumerable<AutomationElement> elements)
		{
			foreach (var element in elements) {
				Dump(element);
			}
		}

		public static AutomationElementCollection FindTextElements(AutomationElement element)
		{
			return element.FindAll(TreeScope.Subtree,
				new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text));
		}

		public static string ToText(AutomationElement window)
		{
			return FindTextElements(window)
				.Cast<AutomationElement>()
				.Implode(e => e.GetName(), Environment.NewLine);
		}


		//это не будет работать на сервере, всего скорее диалоги там даже не отображаются
		public static void HandleOpenFileDialog(string filename)
		{
			var pid = Process.GetCurrentProcess().Id;
			AutomationElement dialog = null;
			AutomationElement input = null;
			WaitHelper.WaitOrFail(10.Second(), () => {
				dialog = AutomationElement.RootElement.FindFirst(TreeScope.Descendants,
					new AndCondition(
						new PropertyCondition(AutomationElement.NameProperty, "Открыть"),
						new PropertyCondition(AutomationElement.ProcessIdProperty, pid),
						new PropertyCondition(AutomationElement.ClassNameProperty, "#32770")));
				if (dialog == null)
					return false;
				input = dialog.FindFirst(TreeScope.Children,
					new AndCondition(new PropertyCondition(AutomationElement.NameProperty, "Имя файла:"),
						new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ComboBox)));
				return input != null;
			}, "Не удалось дождать появления диалога открытия файла");
			input.SetValue(filename);
			var button = dialog.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, "Открыть"));
			button.Invoke();
		}

		public static AutomationElement FindWindow(string name)
		{
			AutomationElement window = null;
			var handle = WinApi.FindWindow(IntPtr.Zero, name);
			if (handle != IntPtr.Zero)
				window = AutomationElement.FromHandle(handle);
			return window;
		}
	}
}
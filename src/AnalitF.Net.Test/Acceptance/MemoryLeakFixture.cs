using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using Microsoft.Test.Input;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Acceptance
{
	//тесты проверяют наличие утечек памяти, для запуска x86 версия windbg должна быть в PATH
	[TestFixture, Ignore("для ручного тестирования")]
	public class MemoryLeakFixture
	{
		private Process process;
		private AutomationElement MainWindow;

		private TimeSpan timeout = TimeSpan.FromSeconds(5);

		[TearDown]
		public void Teardown()
		{
			if (!process.HasExited)
				process.CloseMainWindow();

			process.WaitForExit();
		}

		[Test]
		public void Orders()
		{
			Activate();

			Click("ShowPrice");
			var prices = WaitForElement("Prices");
			DoubleClickCell(prices, 0, 0);
			WaitForElement("Offers").SetFocus();
			Keyboard.Type("1");

			Click("ShowOrders");
			WaitForElement("Orders");
			Click("ShowMnn");
			WaitForElement("Mnns");

			CheckViewModelLeak(typeof(MnnViewModel));
		}

		[Test]
		public void Open_price()
		{
			Activate();

			Click("ShowPrice");
			var prices = WaitForElement("Prices");
			DoubleClickCell(prices, 0, 0);

			WaitForElement("Offers");

			Click("ShowPrice");
			WaitForElement("Prices");

			CheckViewModelLeak(typeof(PriceViewModel));
		}

		[Test]
		public void Switch_catalog()
		{
			Activate();

			Click("ShowCatalog");
			WaitForElement("CatalogNames");

			Toggle("CatalogSearch");
			WaitForElement("Catalogs");
			Toggle("CatalogSearch");
			WaitForElement("CatalogNames");

			CheckViewModelLeak(typeof(CatalogViewModel), typeof(CatalogNameViewModel));
		}

		private void Toggle(string name)
		{
			var element = Find(name);

			if (element == null)
				throw new Exception(String.Format("Не могу найти кнопку {0}", name));

			var invokePattern = (TogglePattern)element.GetCurrentPattern(TogglePattern.Pattern);
			invokePattern.Toggle();
		}

		private static void DoubleClickCell(AutomationElement prices, int row, int column)
		{
			var cell = ((GridPattern)prices.GetCurrentPattern(GridPattern.Pattern)).GetItem(row, column);
			var rect = (Rect)cell.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
			Mouse.MoveTo(new System.Drawing.Point((int)rect.X + 3, (int)rect.Y + 3));
			Mouse.DoubleClick(MouseButton.Left);
		}

		private void CheckViewModelLeak(params Type[] type)
		{
			Click("Collect");
			Thread.Sleep(2000);
			//process.WaitForInputIdle();
			var posibleViewModels = typeof(ShellViewModel).Assembly.GetTypes()
				.Where(t => typeof(BaseScreen).IsAssignableFrom(t))
				.Select(t => t.FullName)
				.ToArray();
			var logs = WindbgHelper.GetHeapDump(process);
			var viewModels = logs.Where(l => posibleViewModels.Contains(l.ClassName)).ToList();
			Assert.That(viewModels.Select(v => v.ClassName).ToArray(),
				Is.EquivalentTo(type.Select(t => t.FullName).ToArray()),
				viewModels.Implode());
		}

		private AutomationElement WaitForElement(string name)
		{
			Wait(() => Find(name) == null);
			return Find(name);
		}

		private void Activate()
		{
			var root = "acceptance";
			var exe = @"..\..\..\AnalitF.Net.Client\bin\Debug";
			Prepare(exe, root);

			process = new Process();
			process.StartInfo.FileName = Path.Combine(exe, "AnalitF.Net.Client.exe");
			process.Start();
			process.EnableRaisingEvents = true;

			Automation.AddAutomationEventHandler(
				WindowPatternIdentifiers.WindowOpenedEvent,
				AutomationElement.RootElement,
				TreeScope.Subtree,
				OnActivated);

			Wait(() => MainWindow == null);
		}

		private void Click(string name)
		{
			var launchButton = Find(name);
			if (launchButton == null)
				throw new Exception(String.Format("Не могу найти кнопку {0}", name));

			var invokePattern = (InvokePattern)launchButton.GetCurrentPattern(InvokePattern.Pattern);
			invokePattern.Invoke();
		}

		private AutomationElement Find(string name)
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

		private static void Prepare(string exe, string root)
		{
			if (!Directory.Exists(root))
				Directory.CreateDirectory(root);

			var files = Directory.GetFiles(exe, "*.exe")
				.Concat(Directory.GetFiles(exe, "*.dll"));
			files.Each(f => File.Copy(f, Path.Combine(root, Path.GetFileName(f)), true));

			CopyDir("share", Path.Combine(root, "share"));
			CopyDir("backup", Path.Combine(root, "data"));
		}

		private static void CopyDir(string src, string dst)
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

		private void Wait(Func<bool> func)
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

		private void OnActivated(object sender, AutomationEventArgs e)
		{
			MainWindow = AutomationElement.FromHandle(process.MainWindowHandle);
		}
	}
}
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
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
	[TestFixture, Explicit]
	public class MemoryLeakFixture : BaseFixture
	{
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

		private void Toggle(string id)
		{
			var element = FindById(id, MainWindow);

			if (element == null)
				throw new Exception(String.Format("Не могу найти кнопку {0}", id));

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
			//при обращение с объекту с помощью ui automation
			//automation - держит ссылки на эти объекты
			//нужно очистить мусор
			MainWindow = null;
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			//не знаю почему именно здесь нужна пауза
			//но если ее нет тогда сборка мусора не удаляет все объекты
			//наверное есть нитка которая что то делает возможно ее запускает wpf
			Thread.Sleep(4000);
			Collect();
			Thread.Sleep(2000);

			Process.WaitForInputIdle();
			var posibleViewModels = typeof(ShellViewModel).Assembly.GetTypes()
				.Where(t => typeof(BaseScreen).IsAssignableFrom(t))
				.Select(t => t.FullName)
				.ToArray();
			var logs = WindbgHelper.GetHeapDump(Process);
			var viewModels = logs.Where(l => posibleViewModels.Contains(l.ClassName)).ToList();
			Assert.That(viewModels.Select(v => v.ClassName).ToArray(),
				Is.EquivalentTo(type.Select(t => t.FullName).ToArray()),
				viewModels.Implode());
		}

		private void Collect()
		{
			Writer.WriteLine("Collect");
		}
	}
}
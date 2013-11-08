using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Automation;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools;
using Common.Tools.Calendar;
using Microsoft.Test.Input;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Acceptance
{
	[TestFixture, Explicit]
	public class UpdateFixture : BaseFixture
	{
		[Test]
		public void Do_not_hold_file_handle()
		{
			HandleDialogs();
			Activate();

			Click("Update");
			AssertUpdate("Обновление завершено успешно.");

			Click("ShowCatalog");
			WaitForElement("CatalogNames");

			Click("Update");
			AssertUpdate("Обновление завершено успешно.");
		}

		[Test, Ignore]
		public void Check_auto_update()
		{
			Click("Update");
			AssertUpdate("Получена новая версия программы");

			//AssertWindowTest("Внимание! Происходит обновление программы.");

			WaitWindow("АналитФАРМАЦИЯ");
			WaitForMessage("Обновление завершено успешно.");
		}

		[Test]
		public void Mnn_search()
		{
			HandleDialogs();
			Activate();

			Click("ShowMnn");
			var mnns = WaitForElement("Mnns");
			mnns.SetFocus();

			Keyboard.Type("амо");
			//активируем поиск
			Keyboard.Press(Key.Return);

			//Нужно подождать пока данные обновятся
			Thread.Sleep(300);
			//выбираем ячейку и пытаемся войти
			ClickCell(mnns, 0, 0);
			Keyboard.Press(Key.Return);

			WaitForElement("CatalogNames");
		}

		[Test]
		public void Send_order()
		{
			HandleDialogs();
			Activate();

			Click("ShowCatalog");
			var mnns = WaitForElement("CatalogNames");
			mnns.SetFocus();
			Keyboard.Press(Key.Return);
			Keyboard.Press(Key.Return);
			var offers = WaitForElement("Offers");
			offers.SetFocus();
			Keyboard.Press(Key.D1);

			//кнопка активируется с задержкой
			Thread.Sleep(700);
			Click("SendOrders");
			AssertUpdate("Отправка заказов завершена успешно.");
		}

		[Test]
		public void Empty_update()
		{
			Directory.Delete(Path.Combine("acceptance", "data"), true);

			Activate();
			WaitMessage("Для начала работы с программой необходимо заполнить учетные данные");

			var dialog = WaitDialog("Настройка");
			Type("Settings_UserName", "test");
			Type("Settings_Password", "123");

			Click("Save", dialog);

			WaitIdle();
			Click("Update", MainWindow);
			AssertUpdate("Обновление завершено успешно.");
		}

		[Test, Ignore]
		public void Restore_grid_settings()
		{
			Activate();
			Click("ShowPrice");
			var prices = WaitForElement("Prices");
			DialogHandle = e => {
				Dump(e);
			};
			RightClick(prices);
			Thread.Sleep(1000);
		}

		private void WaitMessage(string message)
		{
			var window = FindWindow("АналитФАРМАЦИЯ: Внимание") ?? Opened.Timeout(Timeout).First();
			Assert.AreEqual(message, ToText(window));
			ClickByName("ОК", window);
		}

		private static AutomationElement FindWindow(string name)
		{
			AutomationElement window = null;
			var handle = WinApi.FindWindow(IntPtr.Zero, name);
			if (handle != IntPtr.Zero)
				window = AutomationElement.FromHandle(handle);
			return window;
		}

		private AutomationElement WaitDialog(string name)
		{
			return FindWindow(name) ?? Opened.Timeout(Timeout).First();
		}

		private void Type(string el, string text)
		{
			var username = WaitForElement(el);
			username.SetFocus();
			Keyboard.Type(text);
		}

		private void AssertUpdate(string result)
		{
			Timeout = TimeSpan.FromSeconds(50);
			var update = FindWindow("Обмен данными") ?? Opened.Timeout(5.Second()).First();
			Assert.AreEqual("Обмен данными", update.GetName());
			var dialog = Opened.Timeout(50.Second()).First();

			Assert.AreEqual(result, ToText(dialog));
			ClickByName("Закрыть", dialog);
		}

		private AutomationElement WaitWindow()
		{
			return Opened.Timeout(5.Second()).First();
		}

		private void WaitForMessage(string message)
		{
			var window = WaitWindow();
			var text = ToText(window);
			Assert.That(text, Is.StringContaining(message));
			ClickByName("ОК", window);
		}

		private string ToText(AutomationElement window)
		{
			return FindTextElements(window)
				.Cast<AutomationElement>()
				.Implode(e => e.GetName(), Environment.NewLine);
		}
	}

	public static class AutomationHelper
	{
		public static string GetName(this AutomationElement e)
		{
			return (string)e.GetCurrentPropertyValue(AutomationElement.NameProperty);
		}
	}
}
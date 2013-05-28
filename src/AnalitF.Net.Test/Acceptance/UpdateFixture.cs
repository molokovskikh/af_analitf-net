﻿using System;
using System.Linq;
using System.Threading;
using System.Windows.Automation;
using Common.Tools;
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
			SkipUpdateDialog();
			WaitForMessage("Обновление завершено успешно.");

			Click("ShowCatalog");
			WaitForElement("CatalogNames");

			Click("Update");
			SkipUpdateDialog();
			WaitForMessage("Обновление завершено успешно.");
		}

		[Test]
		public void Check_auto_update()
		{
			Click("Update");
			SkipUpdateDialog();
			WaitForMessage("Получена новая версия программы");

			//AssertWindowTest("Внимание! Происходит обновление программы.");

			WaitWindow("АналитФАРМАЦИЯ");
			WaitForMessage("Обновление завершено успешно.");
		}

		[Test]
		public void Mnn_search()
		{
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

		[Test(Description = "После применения\\отмены фильтров в таблице выбранная строка должна быть по центру таблицы")]
		public void Centrify_on_selected_item()
		{
		}

		[Test]
		public void Restore_grid_settings()
		{
			Activate();
			Click("ShowPrice");
			var prices = WaitForElement("Prices");
			WindowHandler = e => {
				Dump(e);
			};
			RightClick(prices);
			Thread.Sleep(1000);
		}

		private void SkipUpdateDialog()
		{
			Timeout = TimeSpan.FromSeconds(50);
			WaitWindow();
		}

		private AutomationElement WaitWindow()
		{
			AutomationElement window = null;
			WindowHandler = e => { window = e; };
			Wait(() => window == null);
			return window;
		}

		private void HandleDialogs()
		{
			WindowHandler = e => {
				//отказываемся от всего
				ClickByName("Нет", e);
			};
		}

		private void WaitForMessage(string message)
		{
			var window = WaitWindow();
			var text = ToText(window);
			ClickByName("ОК", window);
			Assert.That(text, Is.StringContaining(message));
		}

		private string ToText(AutomationElement window)
		{
			return FindTextElements(window)
				.Cast<AutomationElement>()
				.Implode(e => e.GetCurrentPropertyValue(AutomationElement.NameProperty), Environment.NewLine);
		}
	}
}
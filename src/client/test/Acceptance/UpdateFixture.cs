using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Web.Http.SelfHost;
using System.Windows.Automation;
using System.Windows.Forms.VisualStyles;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Test.Integration;
using Common.Tools;
using Common.Tools.Calendar;
using Microsoft.Test.Input;
using NUnit.Framework;
using Test.Support;

namespace AnalitF.Net.Client.Test.Acceptance
{
	[TestFixture, Explicit]
	public class UpdateFixture : BaseFixture
	{
		[TearDown]
		public void TearDown()
		{
			var serviceConfig = IntegrationSetup.serviceConfig;
			Directory.GetFiles(serviceConfig.UpdatePath).Each(File.Delete);
		}

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

		[Test]
		public void Check_auto_update()
		{
			var serviceConfig = IntegrationSetup.serviceConfig;
			File.WriteAllText(Path.Combine(serviceConfig.UpdatePath, "version.txt"), "99.99.99.99");
			DataMother.CopyBin("acceptance", serviceConfig.UpdatePath);
			DataMother.CopyBin(DataMother.ProjectBin("updater"), serviceConfig.UpdatePath);
			AccentanceSetup.Configure("acceptance",
				((HttpSelfHostConfiguration)AccentanceSetup.integrationSetup.server.Configuration).BaseAddress.ToString());

			HandleDialogs();
			Activate();
			Click("Update");
			AssertUpdate("Получена новая версия программы. Сейчас будет выполнено обновление.");

			var update = Opened.Timeout(5.Second()).First();
			AssertText(update, "Внимание! Происходит обновление программы.");

			update = Opened.Where(e => e.GetName() == "Обмен данными").Timeout(15.Second()).First();
			AssertText(update, "Производится обмен данными");
			Process = System.Diagnostics.Process.GetProcessById(update.GetProcessId());
			MainWindow = AutomationElement.RootElement.FindFirst(TreeScope.Descendants, new AndCondition(
				new PropertyCondition(AutomationElement.ProcessIdProperty, Process.Id),
				new PropertyCondition(AutomationElement.NameProperty, "АналитФАРМАЦИЯ")));

			var message = Opened.Timeout(15.Second()).First();
			AssertText(message, "Обновление завершено успешно.");
			ClickByName("ОК", message);
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

		private AutomationElement WaitWindow()
		{
			return Opened.Timeout(5.Second()).First();
		}

		private void WaitForMessage(string message)
		{
			var window = WaitWindow();
			var text = AutomationHelper.ToText(window);
			Assert.That(text, Is.StringContaining(message));
			ClickByName("ОК", window);
		}
	}
}
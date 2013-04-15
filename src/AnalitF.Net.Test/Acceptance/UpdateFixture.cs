using System;
using System.Linq;
using System.Windows.Automation;
using Common.Tools;
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

		private void SkipUpdateDialog()
		{
			Timeout = TimeSpan.FromSeconds(50);
			WaitWindow();
		}

		private AutomationElement WaitWindow()
		{
			AutomationElement window = null;
			DialogHandler = e => { window = e; };
			Wait(() => window == null);
			return window;
		}

		private void HandleDialogs()
		{
			DialogHandler = e => {
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
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using AnalitF.Net.Client;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Test.Integration.ViewModes;
using Caliburn.Micro;
using Castle.Components.DictionaryAdapter;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration
{
	[TestFixture, RequiresSTA]
	public class AppBootstrapperFixture : BaseFixture
	{
		private AppBootstrapper app;

		[SetUp]
		public void Setup()
		{
			app = CreateBootstrapper();
		}

		[TearDown]
		public void TearDown()
		{
			app.Dispose();
		}

		[Test]
		public void Persist_shell()
		{
			File.Delete("AnalitF.Net.Client.data");

			StartShell();
			shell.ViewSettings.Add("test", new List<ColumnSettings> {
				new ColumnSettings()
			});
			app.Serialize();

			StartShell();
			var viewSetting = shell.ViewSettings["test"];
			Assert.That(viewSetting.Count, Is.EqualTo(1));
		}

		[Test]
		public void Serialize_current_address()
		{
			Restore = true;

			session.Save(new Address("Тестовый адрес доставки"));

			StartShell();
			var currentAddress = shell.Addresses.First(a => a.Id != shell.CurrentAddress.Id);
			shell.CurrentAddress = currentAddress;

			app.Serialize();
			var savedAddressId = shell.CurrentAddress.Id;

			StartShell();
			Assert.AreEqual(savedAddressId, shell.CurrentAddress.Id);
			Assert.True(shell.Addresses.Contains(shell.CurrentAddress));
		}

		[Test]
		public void Reject_start_second_time()
		{
			ConfigurationManager.AppSettings["Uri"] = "http://localhost";
			var app = CreateBootstrapper();
			app.Init();
			Assert.IsTrue(app.IsInitialized);

			var app1 = CreateBootstrapper();
			Assert.Throws<EndUserError>(app1.Init);
			Assert.IsFalse(app1.IsInitialized);
		}

		private void StartShell()
		{
			app.InitShell();
			ScreenExtensions.TryActivate(app.Shell);
			shell = app.Shell;
		}

		private AppBootstrapper CreateBootstrapper()
		{
			//нужно переопределить имя что бы избежать конфликтов с запущеным приложением
			var app = new AppBootstrapper(false, "AnalitF.Net.Client.Test");
			Execute.ResetWithoutDispatcher();
			//setup - переопределяет windowmanager но AppBootstrapper вернет все назад
			//нужно восстановить тестовый windowmanager а то тесты начнут показывать окна
			StubWindowManager();
			return app;
		}
	}
}
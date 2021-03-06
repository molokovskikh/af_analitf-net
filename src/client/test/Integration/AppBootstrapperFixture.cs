﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration
{
	[TestFixture]
	public class AppBootstrapperFixture : ViewModelFixture
	{
		private AppBootstrapper app;

		[SetUp]
		public void Setup()
		{
			app = CreateBootstrapper();
			FileHelper.InitDir("test");
			disposable.Add(Disposable.Create(() => {
				FileHelper.Persistent(() => FileHelper.DeleteDir("test"));
			}));
		}

		[Test]
		public void Persist_shell()
		{
			StartShell();
			shell.ViewSettings.Add("test", new List<ColumnSettings> { new ColumnSettings() });
			app.Serialize();

			StartShell();
			var viewSetting = shell.ViewSettings["test"];
			Assert.That(viewSetting.Count, Is.EqualTo(1));
		}

		[Test]
		public void Serialize_current_address()
		{
			restore = true;

			session.Save(new Address("Тестовый адрес доставки"));

			StartShell();
			var currentAddress = shell.Addresses.First(a => a.Id != shell.CurrentAddress.Value.Id);
			shell.CurrentAddress.Value = currentAddress;

			app.Serialize();
			var savedAddressId = shell.CurrentAddress.Value.Id;

			app = CreateBootstrapper();
			StartShell();
			Assert.AreEqual(savedAddressId, shell.CurrentAddress.Value.Id);
			Assert.True(shell.Addresses.Contains(shell.CurrentAddress.Value));
		}

		[Test]
		public void Import_start()
		{
			FileHelper2.InitFile("test/temp/update/offers.txt");

			Assert.IsNull(app.Shell);

			app.Config.Cmd = "import";
			app.InitApp();
			Assert.IsTrue(File.Exists("test/temp/update/offers.txt"));
		}

		[Test]
		public void Repair_and_restart_if_db_broken()
		{
			restore = true;
			File.WriteAllBytes(Path.Combine(config.DbDir, "sentorders.frm"), new byte[0]);
			StartShell();
			Assert.IsTrue(app.Shell.IsActive);
			Assert.IsTrue(app.Shell.IsInitialized);
			Assert.IsTrue(app.IsInitialized);
		}

		private void StartShell()
		{
			manager.WindowViewModelOpened.Subscribe(Activate);
			app.InitApp();
			app.InitShell();
		}

		protected override ShellViewModel shell => app.Shell;

		private AppBootstrapper CreateBootstrapper()
		{
			//нужно переопределить имя что бы избежать конфликтов с запущенным приложением
			var app = new AppBootstrapper(false);
			disposable.Add(app);
			app.Config.RootDir = "test";
			app.Config.DbDir = Path.GetFullPath(IntegrationSetup.clientConfig.DbDir);
			app.Config.SettingsPath = "AnalitF.Net.Client.Test";
			Execute.ResetWithoutDispatcher();
			return app;
		}
	}
}
﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using AnalitF.Net.Client;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration
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
				var count = 0;
				repeat:
				count++;
				try {
					Directory.Delete("test", true);
				}
				catch(IOException e) {
					//System.IO.IOException : Папка не пуста
					if (e.HResult == 0x80070091 && count > 3)
						goto repeat;
					Console.WriteLine("HResult = " + e.HResult);
					throw;
				}
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
			var currentAddress = shell.Addresses.First(a => a.Id != shell.CurrentAddress.Id);
			shell.CurrentAddress = currentAddress;

			app.Serialize();
			var savedAddressId = shell.CurrentAddress.Id;

			app = CreateBootstrapper();
			StartShell();
			Assert.AreEqual(savedAddressId, shell.CurrentAddress.Id);
			Assert.True(shell.Addresses.Contains(shell.CurrentAddress));
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

		private void StartShell()
		{
			app.InitApp();
			app.InitShell();
			Activate(app.Shell);
		}

		protected override ShellViewModel shell
		{
			get { return app.Shell; }
		}

		private AppBootstrapper CreateBootstrapper()
		{
			//нужно переопределить имя что бы избежать конфликтов с запущенным приложением
			var app = new AppBootstrapper(false);
			disposable.Add(app);
			app.Config.RootDir = "test";
			app.Config.SettingsPath = "AnalitF.Net.Client.Test";
			app.Config.IsUnitTesting = true;
			Execute.ResetWithoutDispatcher();
			disposable.Add(app);
			return app;
		}
	}
}
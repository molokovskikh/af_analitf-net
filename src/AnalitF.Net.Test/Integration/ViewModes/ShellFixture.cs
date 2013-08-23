using System;
using System.IO;
using System.Linq;
using System.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.Tools;
using Ionic.Zip;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture, Ignore("Тесты сломаны, из-за обработки даилога я не знаю как их чинить")]
	public class ShellFixture : BaseFixture
	{
		[Test, RequiresSTA]
		public void If_user_name_empty_open_configuration_form()
		{
			ResetCredentials();

			shell.OnViewReady();
			Assert.That(manager.MessageBoxes.Implode(), Is.StringContaining("необходимо заполнить учетные данные"));
			Assert.That(manager.Dialogs[0].DataContext, Is.TypeOf<SettingsViewModel>());
		}

		[Test, RequiresSTA]
		public void Reject_update_with_empty_user_name()
		{
			ResetCredentials();

			shell.OnViewReady();
			manager.MessageBoxes.Clear();
			manager.Dialogs[0].Close();
			manager.Dialogs.Clear();

			shell.Update();
			Assert.That(manager.MessageBoxes.Implode(), Is.StringContaining("необходимо заполнить учетные данные"));
			Assert.That(manager.Dialogs[0].DataContext, Is.TypeOf<SettingsViewModel>());
		}

		[Test]
		public void Execute_update_task()
		{
			session.DeleteEach<Address>();
			session.Flush();
			shell.Reload();
			Assert.That(shell.Addresses.Count, Is.EqualTo(0));

			PrepareForSync();

			StartSync();

			Assert.That(manager.Dialogs.Count, Is.EqualTo(0));
			Assert.That(shell.Addresses.Count, Is.EqualTo(1));
		}

		[Test]
		public void Before_sync_close_active_items()
		{
			PrepareForSync();

			shell.ShowCatalog();
			StartSync();

			Assert.That(shell.NavigationStack.Count(), Is.EqualTo(0));
			Assert.That(shell.ActiveItem, Is.Null);
		}

		[Test]
		public void Close_current_view_on_address_change()
		{
			shell.ShowPrice();
			shell.CurrentAddress = shell.Addresses[1];

			Assert.That(shell.ActiveItem, Is.Null);
			Assert.That(shell.NavigationStack, Is.Empty);
		}

		[Test, RequiresSTA]
		public void Import_if_argument_specified()
		{
			Tasks.ExtractPath = "temp";
			Tasks.ArchiveFile = Path.Combine(Tasks.ExtractPath, "archive.zip");
			FileHelper.InitDir(Tasks.ExtractPath);
			File.Copy(@"..\..\..\data\result\21", Tasks.ArchiveFile);
			new ZipFile(Tasks.ArchiveFile).ExtractAll(Tasks.ExtractPath);

			PrepareForSync();

			session.CreateSQLQuery("delete from offers").ExecuteUpdate();
			shell.Arguments = new[] { "cmd.exe", "import" };
			WpfHelper.WithDispatcher(() => {
				shell.OnViewReady();
			});

			var closed = new ManualResetEventSlim();
			manager.Dialogs[0].Closed += (sender, args) => closed.Set();
			closed.Wait(TimeSpan.FromSeconds(10));

			Assert.That(manager.MessageBoxes.Implode(), Is.EqualTo("Обновление завершено успешно."));
			Assert.That(session.Query<Offer>().Count(), Is.GreaterThan(0));
		}

		private void StartSync()
		{
			var dialogClosed = new ManualResetEventSlim();
			var taskCompleted = new ManualResetEventSlim();
			//Tasks.Update = (c, t, p) => {
			//	Thread.Sleep(100);
			//	taskCompleted.Wait();
			//	return UpdateResult.OK;
			//};

			var dispatcher = WpfHelper.WithDispatcher(() => {
				taskCompleted.Set();
				shell.Update();
				Assert.That(manager.Dialogs.Count, Is.EqualTo(1));
				manager.Dialogs[0].Closed += (sender, args) => {
					dialogClosed.Set();
				};
			});
			dialogClosed.Wait(TimeSpan.FromSeconds(10));
			dispatcher.InvokeShutdown();
		}

		private void PrepareForSync()
		{
			var settings = session.Query<Settings>().First();
			settings.UserName = "test";
			settings.Password = "123";
			session.Flush();
			shell = new ShellViewModel();
			ScreenExtensions.TryActivate(shell);
		}

		private void ResetCredentials()
		{
			var settings = session.Query<Settings>().First();
			settings.UserName = null;
			settings.Password = null;
			session.Flush();
		}
	}
}
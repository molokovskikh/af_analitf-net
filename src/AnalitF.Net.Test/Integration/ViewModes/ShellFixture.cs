using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI;
using Action = System.Action;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class ShellFixture : BaseFixture
	{
		[SetUp]
		public void Setup()
		{
			var settings = session.Query<Settings>().First();
			settings.UserName = null;
			settings.Password = null;
			session.Flush();

			shell = new ShellViewModel();
		}

		[Test]
		public void If_user_name_empty_open_configuration_form()
		{
			((IActivate)shell).Activate();
			Assert.That(manager.MessageBoxes.Implode(), Is.StringContaining("необходимо заполнить учетные данные"));
			Assert.That(shell.ActiveItem, Is.TypeOf<SettingsViewModel>());
		}

		[Test]
		public void Reject_update_with_empty_user_name()
		{
			((IActivate)shell).Activate();
			manager.MessageBoxes.Clear();
			shell.ActiveItem.TryClose();
			shell.Update();
			Assert.That(manager.MessageBoxes.Implode(), Is.StringContaining("необходимо заполнить учетные данные"));
			Assert.That(shell.ActiveItem, Is.TypeOf<SettingsViewModel>());
		}

		[Test]
		public void Execute_update_task()
		{
			PrepareForSync();

			StartSync();

			Assert.That(manager.Dialogs.Count, Is.EqualTo(0));
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

		private void StartSync()
		{
			var waitClose = new ManualResetEventSlim();
			var wait = new ManualResetEventSlim();
			var waittask = new Task<UpdateResult>(() => {
				wait.Wait();
				return UpdateResult.OK;
			});
			Tasks.Update = (c, t, p) => waittask;

			var dispatcher = WithDispatcher(() => {
				shell.Update();
				Assert.That(manager.Dialogs.Count, Is.EqualTo(1));
				manager.Dialogs[0].Closed += (sender, args) => waitClose.Set();
				wait.Set();
			});
			waitClose.Wait(TimeSpan.FromSeconds(10));
			dispatcher.InvokeShutdown();
		}

		private void PrepareForSync()
		{
			var settings = session.Query<Settings>().First();
			settings.UserName = "test";
			settings.Password = "123";
			session.Flush();
			shell = new ShellViewModel();
		}

		public static Dispatcher WithDispatcher(Action action)
		{
			var started = new ManualResetEventSlim();
			var dispatcherThread = new Thread(() => {
				// This is here just to force the dispatcher infrastructure to be setup on this thread
				Dispatcher.CurrentDispatcher.BeginInvoke(new Action(started.Set));

				// Run the dispatcher so it starts processing the message loop
				Dispatcher.Run();
			});

			dispatcherThread.SetApartmentState(ApartmentState.STA);
			dispatcherThread.IsBackground = true;
			dispatcherThread.Start();
			started.Wait();
			var dispatcher = Dispatcher.FromThread(dispatcherThread);
			dispatcher.Invoke(() => {
				RxApp.DeferredScheduler = DispatcherScheduler.Current;
				action();
			});
			return dispatcher;
		}
	}
}
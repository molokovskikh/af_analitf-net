using System;
using System.Linq;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.Integration.ViewModels;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Offers;
using Caliburn.Micro;
using Common.Tools;
using Common.Tools.Threading;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using ReactiveUI;
using ReactiveUI.Testing;
using WindowManager = AnalitF.Net.Client.Config.Caliburn.WindowManager;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	[Apartment(ApartmentState.STA)]
	public class BaseUnitFixture
	{
		private CompositeDisposable cleanup;

		protected FileCleaner cleaner;
		protected TestScheduler scheduler;
		protected MessageBus bus;
		protected WindowManager manager;
		protected ShellViewModel shell;
		protected User user;

		[SetUp]
		public void BaseUnitFixtureSetup()
		{
			cleaner = new FileCleaner();
			cleanup = new CompositeDisposable();
			cleanup.Add(cleaner);
			user = new User();
			bus = new MessageBus();
			scheduler = new TestScheduler();
			Env.Current = new Env(user, bus, scheduler, null/*не нужно использовать базу для этого есть интеграционные тесты*/) {
				//тк в модульных тестах сессия не инициализируется все запросы будут "завершаться" моментально в той же нитке
				QueryScheduler = new CurrentThreadScheduler(),
				TplUiScheduler = new CurrentThreadScheduler(),
				Settings = new Settings()
			};

			manager = ViewModelFixture.StubWindowManager();
			shell = new ShellViewModel();
		}

		[TearDown]
		public void BaseUnitFixtureTearDown()
		{
			//конструируем пустой контекст что бы обращения без явной инициализации привели к ошибкам
			Env.Current = new Env(null, null, null, null);
			cleanup.Dispose();
		}

		protected void Activate(BaseScreen screen, params Address[] addresses)
		{
			var address = addresses.FirstOrDefault() ?? new Address("тест");
			if (addresses.Length == 0) {
				addresses = new[] { address };
			}
			screen.Settings.Value = new Settings(addresses);
			screen.User = user;
			screen.Address = address;
			screen.Parent = shell;
			screen.Addresses = addresses;
			ScreenExtensions.TryActivate(screen);
			shell.ActiveItem = screen;
			shell.CurrentAddress = address;
			shell.Addresses = addresses.ToList();
		}

		protected void Close(object model)
		{
			ScreenExtensions.TryDeactivate(model, false);
		}
	}
}
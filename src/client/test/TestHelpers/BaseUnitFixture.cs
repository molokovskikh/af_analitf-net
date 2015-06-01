using System.Linq;
using System.IO;
using System.Reactive.Disposables;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Offers;
using Caliburn.Micro;
using Common.Tools;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using ReactiveUI;
using ReactiveUI.Testing;
using WindowManager = AnalitF.Net.Client.Config.Caliburn.WindowManager;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class BaseUnitFixture
	{
		private CompositeDisposable cleanup;
		private FileCleaner cleaner;

		protected TestScheduler scheduler;
		protected MessageBus bus;
		protected WindowManager manager;
		protected ShellViewModel shell;
		protected User user;

		[SetUp]
		public void BaseUnitFixtureSetup()
		{
			user = new User();
			cleaner = new FileCleaner();
			cleanup = new CompositeDisposable();
			cleanup.Add(cleaner);
			BaseScreen.UnitTesting = true;
			bus = new MessageBus();
			RxApp.MessageBus = bus;
			scheduler = new TestScheduler();
			BaseScreen.TestSchuduler = scheduler;
			cleanup.Add(TestUtils.WithScheduler(scheduler));

			manager = ViewModelFixture.StubWindowManager();
			var config = new Config.Config {
				IsUnitTesting = true,
				SkipOpenSession = true,
			};
			shell = new ShellViewModel(config);
			shell.Env = new Env {
				IsUnitTesting = true
			};
			BaseScreen.TestContext = new AppTestContext(user);
		}

		[TearDown]
		public void BaseUnitFixtureTearDown()
		{
			BaseScreen.UnitTesting = false;
			BaseScreen.TestContext = null;
			cleanup.Dispose();
		}

		public string RandomFile()
		{
			return cleaner.RandomFile();
		}

		protected void Activate(BaseScreen screen, params Address[] addresses)
		{
			var address = addresses.FirstOrDefault() ?? new Address("тест");
			if (addresses.Length == 0) {
				addresses = new[] { address };
			}
			screen.User = user;
			screen.Address = address;
			screen.Parent = shell;
			screen.Addresses = addresses;
			ScreenExtensions.TryActivate(screen);
		}
	}
}
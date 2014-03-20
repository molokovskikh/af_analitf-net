using System.IO;
using System.Reactive.Disposables;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using ReactiveUI;
using ReactiveUI.Testing;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class BaseUnitFixture
	{
		private CompositeDisposable cleanup;
		private FileCleaner cleaner;

		protected TestScheduler scheduler;
		protected MessageBus bus;
		protected WindowManager manager;

		[SetUp]
		public void Setup()
		{
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
		}

		[TearDown]
		public void TearDown()
		{
			BaseScreen.UnitTesting = false;
			cleanup.Dispose();
		}

		public string RandomFile()
		{
			return cleaner.RandomFile();
		}
	}
}
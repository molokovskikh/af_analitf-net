using System.Reactive.Disposables;
using AnalitF.Net.Client.ViewModels;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using ReactiveUI;
using ReactiveUI.Testing;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class BaseUnitFixture
	{
		private TestScheduler scheduler;
		private CompositeDisposable cleanup;

		[SetUp]
		public void Setup()
		{
			cleanup = new CompositeDisposable();
			BaseScreen.UnitTesting = true;
			RxApp.MessageBus = new MessageBus();
			scheduler = new TestScheduler();
			BaseScreen.TestSchuduler = scheduler;
			cleanup.Add(TestUtils.WithScheduler(scheduler));

			ViewModelFixture.StubWindowManager();
		}

		[TearDown]
		public void TearDown()
		{
			BaseScreen.UnitTesting = false;
			cleanup.Dispose();
		}
	}
}
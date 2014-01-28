using System.Reactive.Disposables;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using ReactiveUI;
using ReactiveUI.Testing;

namespace AnalitF.Net.Test.Unit.ViewModels
{
	[TestFixture]
	public class CatalogNameFixture
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

		[Test]
		public void Set_catalog()
		{
			var catalog = new CatalogNameViewModel(new CatalogViewModel());
			catalog.CurrentCatalog = new Catalog("тест");
			catalog.CurrentCatalogName.Value = null;
			catalog.CurrentCatalog = null;
		}
	}
}
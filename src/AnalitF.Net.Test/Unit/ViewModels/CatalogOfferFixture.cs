using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.Acceptance;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using ReactiveUI;
using ReactiveUI.Testing;

namespace AnalitF.Net.Test.Unit.ViewModels
{
	[TestFixture]
	public class CatalogOfferFixture
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
		public void Recalculate_on_offer_changed()
		{
			var model = new CatalogOfferViewModel(new Catalog("Тестовый"));
			model.User = new User();
			model.Offers.Value = new List<Offer> {
				new Offer(new Price("test1"), 100) {
					Id = {
						OfferId = 1
					}
				},
				new Offer(new Price("test2"), 150) {
					Id = {
						OfferId = 2
					}
				}
			};
			model.CurrentOffer.Value = model.Offers.Value[0];
			Assert.AreEqual(model.RetailMarkup.Value, 20);
			Assert.AreEqual(model.RetailCost.Value, 120);
			model.CurrentOffer.Value = model.Offers.Value[1];
			Assert.AreEqual(model.RetailCost.Value, 180);
		}
	}
}
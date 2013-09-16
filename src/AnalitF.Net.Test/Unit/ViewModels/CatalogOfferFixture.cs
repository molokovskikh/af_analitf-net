using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using ReactiveUI;
using ReactiveUI.Testing;
using WindowManager = AnalitF.Net.Client.Extentions.WindowManager;

namespace AnalitF.Net.Test.Unit.ViewModels
{
	[TestFixture]
	public class CatalogOfferFixture
	{
		private TestScheduler scheduler;
		private CompositeDisposable cleanup;
		private WindowManager manager;

		[SetUp]
		public void Setup()
		{
			cleanup = new CompositeDisposable();
			BaseScreen.UnitTesting = true;
			RxApp.MessageBus = new MessageBus();
			scheduler = new TestScheduler();
			BaseScreen.TestSchuduler = scheduler;
			cleanup.Add(TestUtils.WithScheduler(scheduler));

			manager = new WindowManager();
			manager.UnderTest = true;
			var @base = IoC.GetInstance;
			IoC.GetInstance = (type, key) => {
				if (type == typeof(IWindowManager))
					return manager;
				return @base(type, key);
			};
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
			model.Settings.Value.Markups = MarkupConfig.Defaults().ToArray();
			model.Offers = new List<Offer> {
				new Offer(new Price {
					Id = new PriceComposedId()
				}, 100) {
					Id = {
						OfferId = 1
					}
				},
				new Offer(new Price {
					Id = new PriceComposedId()
				}, 150) {
					Id = {
						OfferId = 2
					}
				}
			};
			model.CurrentOffer = model.Offers[0];
			Assert.AreEqual(model.RetailMarkup.Value, 20);
			Assert.AreEqual(model.RetailCost.Value, 120);
			model.CurrentOffer = model.Offers[1];
			Assert.AreEqual(model.RetailCost.Value, 180);
		}
	}
}
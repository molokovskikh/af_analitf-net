using System.Collections.Generic;
using System.Reactive.Disposables;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using ReactiveUI;
using ReactiveUI.Testing;

namespace AnalitF.Net.Test.Unit.ViewModels
{
	public class PriceOfferFixture : BaseUnitFixture
	{
		private PriceOfferViewModel model;
		private Price price;

		[SetUp]
		public void Setup()
		{
			price = new Price("test1");
			model = new PriceOfferViewModel(price.Id, false);
			model.Address = new Address("test addr");
			model.Price.Value = price;
		}

		[Test]
		public void Update_filter_on_order_edit()
		{
			model.PriceOffers = new List<Offer> {
				new Offer(price, 100),
				new Offer(price, 200),
				new Offer(price, 15)
			};
			ScreenExtensions.TryActivate(model);
			OrderByIndex(model, 0);
			OrderByIndex(model, 2);

			model.FilterOrdered();
			Assert.AreEqual(2, model.Offers.Value.Count);

			OrderByIndex(model, 0, 0);
			Assert.AreEqual(1, model.Offers.Value.Count);

			model.DeleteOrder();
			Assert.AreEqual(0, model.Offers.Value.Count);
		}

		[Test]
		public void SearchOffers()
		{
			model.PriceOffers = new List<Offer> {
				new Offer(price, 100) {
					ProductSynonym = "Папаверинг",
					ProducerSynonym = "FARAN",
					Producer = "FARAN"
				},
				new Offer(price, 200) {
					ProductSynonym = "Аллохол",
					ProducerSynonym = "ARKRAY",
					Producer = "ARKRAY"
				},
			};
			ScreenExtensions.TryActivate(model);
			model.SearchBehavior.SearchText.Value = "Папаверинг";
			model.Search();
			Assert.AreEqual(1, model.Offers.Value.Count);
		}

		[Test]
		public void Load_producers()
		{
			model.PriceOffers = new List<Offer> {
				new Offer(price, 100) {
					ProductSynonym = "Папаверинг",
					ProducerSynonym = "FARAN",
					Producer = "FARAN",
					ProducerId = 1
				},
				new Offer(price, 200) {
					ProductSynonym = "Аллохол",
					ProducerSynonym = "ARKRAY",
					Producer = "ARKRAY",
					ProducerId = 2
				},
			};
			ScreenExtensions.TryActivate(model);
			Assert.AreEqual(3, model.Producers.Value.Count);
		}

		private static void OrderByIndex(PriceOfferViewModel model, int index, uint count = 1)
		{
			model.CurrentOffer.Value = model.Offers.Value[index];
			model.CurrentOffer.Value.OrderCount = count;
			model.OfferUpdated();
			model.OfferCommitted();
		}
	}
}
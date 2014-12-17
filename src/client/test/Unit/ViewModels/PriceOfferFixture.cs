using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Offers;
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
			var offers = new List<Offer> {
				new Offer(price, 100),
				new Offer(price, 200),
				new Offer(price, 15)
			};
			Activate(offers);
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
			var offers = new List<Offer> {
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
			Activate(offers);
			model.SearchBehavior.SearchText.Value = "Папаверинг";
			model.SearchBehavior.Search();
			Assert.AreEqual(1, model.Offers.Value.Count);
		}

		[Test]
		public void Load_producers()
		{
			var offers = new List<Offer> {
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
			Activate(offers);
			model.FillProducerFilter(model.PriceOffers);
			Assert.AreEqual(3, model.Producers.Value.Count);
		}

		[Test]
		public void Do_not_order_from_forbidden_prices()
		{
			var price = new Price("test1");
			model.Offers.Value = new List<Offer> {
				new Offer(price, 100) {
					Id = {
						OfferId = 1
					},
				},
			};
			price.IsOrderDisabled = true;
			model.CurrentOffer.Value = model.Offers.Value.First();
			model.CurrentOffer.Value.OrderCount = 1;
			model.OfferUpdated();
			Assert.IsNull(model.CurrentOffer.Value.OrderCount);
			Assert.IsNull(model.CurrentOffer.Value.OrderLine);
		}

		private void Activate(List<Offer> offers)
		{
			ScreenExtensions.TryActivate(model);
			model.BindOffers(offers);
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
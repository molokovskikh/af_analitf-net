using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Offers;
using Common.NHibernate;
using Common.Tools;
using NUnit.Framework;
using ReactiveUI.Testing;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class SearchOfferFixture : ViewModelFixture<SearchOfferViewModel>
	{
		[Test]
		public void Full_filter_values()
		{
			Assert.AreEqual("Все производители", model.Producers.Value[0].Name);
			Assert.That(model.Producers.Value.Count, Is.GreaterThan(0));
			Assert.That(model.Prices.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Filter_base_offers()
		{
			var catalog = FindMultiOfferCatalog();
			MakeDifferentCategory(catalog);

			model.SearchBehavior.SearchText.Value = catalog.Name.Name.Slice(3);
			scheduler.Start();

			var originCount = model.Offers.Value.Count;
			Assert.That(originCount, Is.GreaterThan(0));

			model.OnlyBase.Value = true;
			scheduler.Start();
			Assert.That(model.Offers.Value.Count, Is.LessThan(originCount));
			foreach (var offer in model.Offers.Value) {
				Assert.That(offer.Price.BasePrice, Is.True);
			}
		}

		[Test]
		public void Filter_by_price()
		{
			var catalog = FindMultiOfferCatalog();
			model.SearchBehavior.SearchText.Value = catalog.Name.Name.Slice(3);
			scheduler.Start();

			var id = model.Offers.Value[0].Price.Id;
			model.Prices.Each(p => p.IsSelected = false);
			scheduler.AdvanceByMs(10000);
			Assert.AreEqual(0, model.Offers.Value.Count);

			model.Prices.First(p => p.Item.Id == id).IsSelected = true;
			model.Prices.First(p => p.Item.Id != id).IsSelected = true;
			scheduler.AdvanceByMs(10000);
			Assert.That(model.Offers.Value.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Build_order()
		{
			session.DeleteEach<Order>();
			var order = MakeOrder();

			var catalog = session.Load<Catalog>(order.Lines[0].CatalogId);
			model.SearchBehavior.SearchText.Value = catalog.Name.Name.Slice(3);
			model.SearchBehavior.Search();
			scheduler.Start();
			Assert.That(model.Offers.Value.Count, Is.GreaterThan(0));

			var offer = model.Offers.Value.First(o => o.Id == order.Lines[0].OfferId);
			Assert.AreEqual(1, offer.OrderCount);
		}
	}
}
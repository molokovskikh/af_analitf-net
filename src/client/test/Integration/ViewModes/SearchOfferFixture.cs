using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using NHibernate.Linq;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using ReactiveUI.Testing;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class SearchOfferFixture : ViewModelFixture<SearchOfferViewModel>
	{
		[Test]
		public void Full_filter_values()
		{
			Assert.That(model.Producers.Value.Count, Is.GreaterThan(0));
			Assert.That(model.Prices.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Filter_base_offers()
		{
			var catalog = FindMultiOfferCatalog();
			MakeDifferentCategory(catalog);

			model.SearchBehavior.SearchText.Value = catalog.Name.Name.Slice(3);
			model.Search();

			var originCount = model.Offers.Value.Count;
			Assert.That(originCount, Is.GreaterThan(0));

			model.OnlyBase.Value = true;
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
			model.Search();

			var id = model.Offers.Value[0].Price.Id;
			model.Prices.Each(p => p.IsSelected = false);
			testScheduler.AdvanceByMs(10000);
			Assert.AreEqual(0, model.Offers.Value.Count());

			model.Prices.First(p => p.Item.Id == id).IsSelected = true;
			model.Prices.First(p => p.Item.Id != id).IsSelected = true;
			testScheduler.AdvanceByMs(10000);
			Assert.That(model.Offers.Value.Count(), Is.GreaterThan(0));
		}
	}
}
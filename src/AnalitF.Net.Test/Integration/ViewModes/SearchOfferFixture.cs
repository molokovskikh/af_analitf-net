using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class SearchOfferFixture : BaseFixture
	{
		private SearchOfferViewModel model;

		[SetUp]
		public void Setup()
		{
			model = new SearchOfferViewModel();
		}

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

			model.SearchText = catalog.Name.Name.Slice(3);
			model.Search();

			var originCount = model.Offers.Count;
			Assert.That(originCount, Is.GreaterThan(0));

			model.OnlyBase.Value = true;
			Assert.That(model.Offers.Count, Is.LessThan(originCount));
			foreach (var offer in model.Offers) {
				Assert.That(offer.Price.BasePrice, Is.True);
			}
		}
	}
}
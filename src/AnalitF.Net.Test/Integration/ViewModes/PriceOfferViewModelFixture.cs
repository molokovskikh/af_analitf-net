using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class PriceOfferViewModelFixture : BaseFixture
	{
		[Test]
		public void Show_catalog()
		{
			var price = session.Query<Price>().First();
			var model = Init(new PriceOfferViewModel(price, false));

			var offer = model.CurrentOffer;
			model.ShowCatalog();

			Assert.That(shell.NavigationStack.Count(), Is.EqualTo(1));
			var catalogModel = (CatalogViewModel)shell.NavigationStack.First();
			Assert.That(catalogModel.CurrentCatalog.Id, Is.EqualTo(offer.CatalogId));
			Assert.That(catalogModel.CurrentCatalogName.Id, Is.EqualTo(catalogModel.CurrentCatalog.Name.Id));

			var offerModel = (CatalogOfferViewModel)shell.ActiveItem;
			Assert.That(offerModel.CurrentOffer.Id, Is.EqualTo(offer.Id));
		}

		[Test]
		public void Show_catalog_with_mnn_filter()
		{
			var offer = session.Query<Offer>().First(o => session.Query<Catalog>().Where(c => c.HaveOffers && c.Name.Mnn != null).Select(c => c.Id).Contains(o.CatalogId));
			var price = session.Load<Price>(offer.Price.Id);
			var model = Init(new PriceOfferViewModel(price, false));
			model.CurrentOffer = model.Offers.First(o => o.Id == offer.Id);
			model.ShowCatalogWithMnnFilter();
			Assert.That(shell.NavigationStack.Count(), Is.EqualTo(0));
			var catalog = (CatalogViewModel)shell.ActiveItem;
			Assert.That(catalog.FilterByMnn, Is.True);
			Assert.That(catalog.FiltredMnn, Is.EqualTo(model.CurrentCatalog.Name.Mnn));
		}
	}
}
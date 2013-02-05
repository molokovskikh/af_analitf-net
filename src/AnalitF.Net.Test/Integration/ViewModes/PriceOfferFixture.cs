using System;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class PriceOfferFixture : BaseFixture
	{
		Lazy<PriceOfferViewModel> lazyModel;
		Price price;

		private PriceOfferViewModel model
		{
			get { return lazyModel.Value; }
		}

		[SetUp]
		public void Setup()
		{
			price = session.Query<Price>().First(p => p.PositionCount > 0);
			lazyModel = new Lazy<PriceOfferViewModel>(
				() => Init(new PriceOfferViewModel(price, false)));
		}


		[Test]
		public void Show_catalog()
		{
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
			var offer = session.Query<Offer>().First(o => session.Query<Catalog>()
				.Where(c => c.HaveOffers && c.Name.Mnn != null)
				.Select(c => c.Id)
				.Contains(o.CatalogId));
			price = session.Load<Price>(offer.Price.Id);

			model.CurrentOffer = model.Offers.First(o => o.Id == offer.Id);
			model.ShowCatalogWithMnnFilter();
			Assert.That(shell.NavigationStack.Count(), Is.EqualTo(0));
			var catalog = (CatalogViewModel)shell.ActiveItem;
			Assert.That(catalog.FilterByMnn, Is.True);
			Assert.That(catalog.FiltredMnn, Is.EqualTo(model.CurrentCatalog.Name.Mnn));
		}

		[Test]
		public void Filter()
		{
			session.DeleteEach<Order>();
			session.Flush();

			model.CurrentFilter = model.Filters[1];
			Assert.That(model.Offers.Count, Is.EqualTo(0));
		}

		[Test]
		public void Show_history_orders()
		{
			MakeSentOrder(model.Offers.First());

			var history = (DialogResult)model.ShowHistoryOrders();
			var lines = ((HistoryOrdersViewModel)history.Model).Lines;
			Assert.That(lines.Count, Is.GreaterThan(0));
		}
	}
}
using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.ViewModels.Orders;
using Common.NHibernate;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class PriceOfferFixture : ViewModelFixture
	{
		Lazy<PriceOfferViewModel> lazyModel;
		Price price;

		private PriceOfferViewModel model => lazyModel.Value;

		[SetUp]
		public void Setup()
		{
			price = session.Query<Price>().First(p => p.PositionCount > 0);
			lazyModel = new Lazy<PriceOfferViewModel>(
				() => {
					var model = new PriceOfferViewModel(price.Id, false);
					Init(model);
					scheduler.Start();
					return model;
				});
		}

		[Test]
		public void Show_catalog()
		{
			var offer = model.CurrentOffer.Value;
			model.ShowCatalog();

			Assert.That(shell.NavigationStack.Count(), Is.EqualTo(1));
			var catalogModel = (CatalogViewModel)shell.NavigationStack.First();
			Assert.That(catalogModel.CurrentCatalog.Id, Is.EqualTo(offer.CatalogId));
			Assert.That(catalogModel.CurrentCatalogName.Id, Is.EqualTo(catalogModel.CurrentCatalog.Name.Id));

			var offerModel = (CatalogOfferViewModel)shell.ActiveItem;
			Assert.That(offerModel.CurrentOffer.Value.Id, Is.EqualTo(offer.Id));
		}

		[Test]
		public void Show_catalog_with_mnn_filter()
		{
			var offer = session.Query<Offer>().First(o => session.Query<Catalog>()
				.Where(c => c.HaveOffers && c.Name.Mnn != null)
				.Select(c => c.Id)
				.Contains(o.CatalogId));
			price = session.Load<Price>(offer.Price.Id);
			model.CurrentOffer.Value = model.Offers.Value.First(o => o.Id == offer.Id);
			scheduler.Start();
			Assert.IsTrue(model.CanShowCatalogWithMnnFilter.Value,
				$"model.CurrentCatalog.Value.Id = {model.CurrentCatalog.Value?.Id}, offer.Id = {offer.Id}");
			model.ShowCatalogWithMnnFilter();

			var catalog = (CatalogViewModel)shell.ActiveItem;
			Assert.That(shell.NavigationStack.Count(), Is.EqualTo(0), shell.NavigationStack.Implode());
			Assert.That(catalog.FilterByMnn, Is.True);
			Assert.That(catalog.FiltredMnn, Is.EqualTo(model.CurrentCatalog.Value.Name.Mnn));
		}

		[Test]
		public void Filter()
		{
			session.DeleteEach<Order>();
			session.Flush();

			model.CurrentFilter.Value = model.Filters[1];
			Assert.That(model.Offers.Value.Count, Is.EqualTo(0));
		}

		[Test]
		public void Show_history_orders()
		{
			var offer = session.Query<Offer>().OrderBy(o => o.ProductSynonym).First(o => o.Price == price);
			MakeSentOrder(offer);

			model.CurrentOffer.Value = model.Offers.Value.First(o => o.Id == offer.Id);
			var history = (DialogResult)model.ShowHistoryOrders();
			var lines = ((HistoryOrdersViewModel)history.Model).Lines;
			Assert.That(lines.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Delete_order()
		{
			session.DeleteEach<Order>();
			var offer = session.Query<Offer>().First(o => o.Price == price);
			var order = MakeOrder(offer);

			Assert.That(model.Price.Value.Order, Is.Not.Null);
			Assert.That(model.Offers.Value.First(o => o.Id == offer.Id).OrderLine, Is.Not.Null);
			model.DeleteOrder();
			Assert.That(model.Price.Value.Order, Is.Null);
			Assert.That(model.Offers.Value.First(o => o.Id == offer.Id).OrderLine, Is.Null);

			Close(model);
			session.Clear();
			Assert.That(session.Get<Order>(order.Id), Is.Null);
		}

		[Test]
		public void Filter_by_ordered()
		{
			session.DeleteEach<Order>();

			model.CurrentOffer.Value.OrderCount = 1;
			model.OfferUpdated();
			model.OfferCommitted();

			model.CurrentFilter.Value = model.Filters[1];
			Assert.That(model.Offers.Value.Count, Is.EqualTo(1));
		}
	}
}
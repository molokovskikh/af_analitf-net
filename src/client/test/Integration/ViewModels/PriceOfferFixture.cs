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
				() => Open(new PriceOfferViewModel(price.Id, false)));
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
			Assert.AreEqual(1, shell.NavigationStack.Count(), shell.NavigationStack.Implode());
			Assert.AreEqual(model, shell.NavigationStack.Last());
			Assert.That(catalog.FilterByMnn, Is.True);
			Assert.That(catalog.FiltredMnn, Is.EqualTo(model.CurrentCatalog.Value.Name.Mnn));
		}

		[Test]
		public void Filter_by_producer_SavingState()
		{
			//проверяем наличие флагов после сохранения фильтра
			Assert.That(model.CanSaveFilterProducer.Value, Is.EqualTo(false));
			Assert.That(model.CurrentProducer.Value.Id, Is.EqualTo(0));
			//выставляем флаг "сохранения фильтра"
			model.CanSaveFilterProducer.Value = true;
			model.Filter();
			//проверяем количемтво выводимых записей при не заполненном фильтре
			var maxCount = model.Offers.Value.Count;
			Assert.That(model.Offers.Value.Count, Is.EqualTo(maxCount));

			//устанавливаем фильтрацию по одному поставщику
			model.CurrentProducer.Value = model.Producers.Value[1];
			Assert.That(model.Offers.Value.Count, Is.LessThan(maxCount));
			//закрываем форму
			model.TryClose();
			//проверяем наличие флагов после сохранения фильтра
			Assert.That(model.CanSaveFilterProducer.Value, Is.EqualTo(true));
			Assert.That(model.CurrentProducer.Value.Id, Is.Not.EqualTo(0));
			//закрываем форму
			model.TryClose();
			Close(model);
			var modelNew = Open(new PriceOfferViewModel(price.Id, false));

			modelNew.Filter();
			//проверяем количемтво выводимых записей при заполненном фильтре на новой форме
			Assert.That(modelNew.Offers.Value.Count, Is.LessThan(maxCount));
			modelNew.Filter();

			//проверяем наличие флагов после сохранения фильтра
			Assert.That(modelNew.CanSaveFilterProducer.Value, Is.EqualTo(true));
			Assert.That(modelNew.CurrentProducer.Value.Id, Is.Not.EqualTo(0));

			//убираем флаг "сохранения фильтра"
			modelNew.CanSaveFilterProducer.Value = false;
			modelNew.CurrentProducer.Value = modelNew.Producers.Value.FirstOrDefault(s => s.Id == 0);
			modelNew.Filter();
			//проверяем количемтво выводимых записей при заполненном фильтре на новой форме
			Assert.That(modelNew.Offers.Value.Count, Is.EqualTo(maxCount));
			//закрываем форму
			modelNew.TryClose();

			//проверяем наличие флагов после сохранения фильтра
			Assert.That(modelNew.CanSaveFilterProducer.Value, Is.EqualTo(false));
			Assert.That(modelNew.CurrentProducer.Value.Id, Is.EqualTo(0));
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

			model.CurrentOffer.Value.OrderCount = model.CurrentOffer.Value.RequestRatio ?? 1;
			model.OfferUpdated();
			model.OfferCommitted();

			model.CurrentFilter.Value = model.Filters[1];
			Assert.That(model.Offers.Value.Count, Is.EqualTo(1));
		}
	}
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;
using Common.NHibernate;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;
using AnalitF.Net.Client.Helpers;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class CatalogOfferFixture : BaseFixture
	{
		private Lazy<CatalogOfferViewModel> lazyModel;
		private Catalog catalog;

		private CatalogOfferViewModel model
		{
			get { return lazyModel.Value; }
		}

		[SetUp]
		public void Setup()
		{
			lazyModel = new Lazy<CatalogOfferViewModel>(() => {
				session.Flush();
				return Init(new CatalogOfferViewModel(catalog));
			});
			catalog = session.Query<Catalog>()
				.First(c => c.HaveOffers && session.Query<Offer>().Count(o => o.CatalogId == c.Id) >= 2);
		}

		[Test]
		public void Filter_by_producer()
		{
			restore = true;

			var baseOffer = session.Query<Offer>().First(o => o.CatalogId == catalog.Id);
			var newOffer = new Offer(baseOffer.Price, baseOffer, baseOffer.Cost + 50) {
				Producer = "Тестовый",
				ProducerId = (uint?)Generator.Random(int.MaxValue).First()
			};
			newOffer.Id.OfferId += (ulong)Generator.Random(int.MaxValue).First();
			session.Save(newOffer);

			var count = model.Offers.Count;
			Assert.That(count, Is.GreaterThan(2));
			model.CurrentProducer.Value = model.Producers.Value[1];
			Assert.That(model.Offers.Count, Is.LessThan(count));
		}

		[Test]
		public void Calculate_retail_cost()
		{
			var splitCost = session.Query<Offer>()
				.Where(o => o.CatalogId == catalog.Id)
				.OrderBy(o => o.Cost)
				.First().Cost;

			settings.Markups.Clear();
			var markupType = catalog.VitallyImportant ? MarkupType.VitallyImportant : MarkupType.Over;
			settings.Markups.Add(new MarkupConfig(0, splitCost, 20, markupType));
			settings.Markups.Add(new MarkupConfig(splitCost, 100 * splitCost, 30, markupType));
			session.Save(settings);

			Assert.That(model.Offers[0].RetailCost, Is.Not.EqualTo(0));

			model.CurrentOffer = model.Offers[0];
			Assert.That(model.RetailMarkup.Value, Is.EqualTo(20));
			var expected = Math.Round(model.Offers[0].Cost * (decimal)1.2, 2);
			Assert.That(model.RetailCost.Value, Is.EqualTo(expected));

			model.CurrentOffer = model.Offers[1];
			Assert.That(model.RetailMarkup.Value, Is.EqualTo(30), "цена разделитель {0} текущая {1}", splitCost, model.CurrentOffer.Cost);
			expected = Math.Round(model.Offers[1].Cost * (decimal)1.3, 2);
			Assert.That(model.RetailCost.Value, Is.EqualTo(expected));

			model.RetailMarkup.Value = 23;
			model.CurrentOffer = model.Offers[0];
			Assert.That(model.RetailMarkup.Value, Is.EqualTo(23));
		}

		[Test]
		public void Calculate_diff()
		{
			Assert.That(model.Offers[0].Diff, Is.Null);
			Assert.That(model.Offers[1].Diff, Is.Not.EqualTo(0));
		}

		[Test]
		public void Select_base_offer()
		{
			catalog = FindMultiOfferCatalog();
			MakeDifferentCategory(catalog);

			var baseOffer = model.Offers.First(o => o.Price.BasePrice);
			Assert.That(model.CurrentOffer.Id, Is.EqualTo(baseOffer.Id), model.Offers.Implode(o => o.Id));
		}

		[Test]
		public void Filter_by_price_category()
		{
			catalog = FindMultiOfferCatalog();
			MakeDifferentCategory(catalog);

			var count = model.Offers.Count;
			model.CurrentFilter.Value = model.Filters[1];
			Assert.That(model.Offers.Count, Is.EqualTo(1));
			Assert.That(model.Offers[0].Price.BasePrice, Is.True);

			model.CurrentFilter.Value = model.Filters[0];
			Assert.That(model.Offers.Count, Is.EqualTo(count));

			model.CurrentFilter.Value = model.Filters[2];
			Assert.That(model.Offers.Count, Is.EqualTo(count - 1));
			Assert.That(model.Offers[0].Price.BasePrice, Is.False, model.Offers[0].Price.Id.ToString());
		}

		[Test]
		public void Filter_result_empty()
		{
			var ids = model.Offers.Select(o => o.Price.Id).Distinct().ToList();
			var prices = session.Query<Price>().ToList().Where(p => ids.Contains(p.Id)).ToList();

			foreach (var price in prices)
				price.BasePrice = true;
			session.Flush();

			model.CurrentFilter.Value = model.Filters[2];
		}

		[Test]
		public void Load_max_producer_costs()
		{
			//предельные цены формируются только для прайса которые был создан в текущем импорте
			//и для прайсов созданых в предыдущих импортах он не будет сформирован
			var currentPrice = session.Query<Price>().Select(p => p.Id).ToArray().Max();
			var catalogId = session.Query<Offer>().First(o => o.Price.Id == currentPrice && o.VitallyImportant).CatalogId;
			catalog = session.Load<Catalog>(catalogId);

			Assert.That(model.MaxProducerCosts.Value.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Load_history_orders()
		{
			var offer = model.CurrentOffer;

			CleanSendOrders(offer);

			var order = new Order(offer.Price, address);
			order.AddLine(offer, 1);
			var sentOrder = new SentOrder(order);
			session.Save(sentOrder);
			session.Flush();

			testScheduler.AdvanceToMs(4000);

			Assert.That(model.HistoryOrders.Count, Is.EqualTo(1), model.HistoryOrders.Implode(l => l.Id));
			Assert.That(model.HistoryOrders[0].Count, Is.EqualTo(1));
			Assert.That(model.CurrentOffer.PrevOrderAvgCost, Is.EqualTo(offer.Cost));
			Assert.That(model.CurrentOffer.PrevOrderAvgCount, Is.EqualTo(1));
		}

		[Test]
		public void Change_sort()
		{
			model.Offers = new List<Offer> {
				new Offer {
					CatalogId = 1,
					ProductId = 2,
					Cost = 120
				},
				new Offer {
					CatalogId = 1,
					ProductId = 3,
					Cost = 105
				},
				new Offer {
					CatalogId = 1,
					ProductId = 3,
					Cost = 103
				},
				new Offer {
					CatalogId = 1,
					ProductId = 2,
					Cost = 90
				}
			};
			model.GroupByProduct.Value = true;
			Assert.That(model.Offers.Select(o => o.Cost).Implode(), Is.EqualTo("90, 120, 103, 105"));
		}

		[Test]
		public void Load_order_history()
		{
			session.DeleteEach<SentOrder>();

			CleanSendOrders(model.CurrentOffer);
			model.LoadHistoryOrders();
		}

		[Test, Ignore]
		public void Load_order_history_without_address()
		{
			session.DeleteEach<Order>();
			session.DeleteEach<SentOrder>();
			session.DeleteEach<Address>();

			model.LoadHistoryOrders();
		}

		[Test]
		public void Load_order_count()
		{
			model.CurrentOffer.OrderCount = 1;
			model.OfferUpdated();
			model.OfferCommitted();
			Close(model);

			var renewModel = Init(new CatalogOfferViewModel(catalog));
			Assert.That(renewModel.CurrentOffer.OrderCount, Is.EqualTo(1));
			Assert.That(renewModel.CurrentOffer.OrderLine, Is.Not.Null);
			Assert.That(renewModel.CurrentOrder, Is.Not.Null);
		}

		[Test]
		public void Can_not_make_order_if_current_address_is_null()
		{
			shell.CurrentAddress = null;
			model.CurrentOffer.OrderCount = 1;
			model.OfferUpdated();
			model.OfferCommitted();

			Assert.That(model.CurrentOffer.OrderCount, Is.Null);
			Assert.That(model.CurrentOffer.OrderLine, Is.Null);
		}

		[Test]
		public void Create_order_line_with_comment()
		{
			model.AutoCommentText = "тестовый комментарий";
			model.CurrentOffer.OrderCount = 1;
			model.OfferUpdated();
			Assert.That(model.CurrentOffer.OrderLine.Comment, Is.EqualTo("тестовый комментарий"));
		}

		[Test]
		public void Reset_auto_comment()
		{
			model.AutoCommentText = "тестовый комментарий";
			model.ResetAutoComment = true;
			model.CurrentOffer.OrderCount = 1;
			model.OfferUpdated();
			model.OfferCommitted();
			Assert.That(model.CurrentOffer.OrderLine.Comment, Is.EqualTo("тестовый комментарий"));

			model.CurrentOffer = model.Offers[1];
			Assert.That(model.AutoCommentText, Is.Null);

			model.CurrentOffer = model.Offers[0];
			Assert.That(model.AutoCommentText, Is.EqualTo("тестовый комментарий"));
		}

		[Test]
		public void Do_not_reset_navigation_chain_on_orders()
		{
			shell.ShowCatalog();
			var catalog = (CatalogViewModel)shell.ActiveItem;
			var names = (CatalogNameViewModel)catalog.ActiveItem;
			names.CurrentCatalog = names.Catalogs.Value[0];
			names.EnterCatalog();
			shell.ShowOrders();
			Assert.That(shell.NavigationStack.Count(), Is.EqualTo(2));
		}

		[Test]
		public void Check_prev_order_count()
		{
			var offer = model.Offers.First();
			MakeSentOrder(offer);

			model.CurrentOffer = offer;
			model.CurrentOffer.OrderCount = 51;
			model.OfferUpdated();
			Assert.That(model.OrderWarning.OrderWarning, Is.EqualTo("Превышение среднего заказа!"));
		}

		[Test]
		public void Reject_activate_if_offers_not_found()
		{
			catalog = session.Query<Catalog>().First(c => !c.HaveOffers);
			Assert.That(model.IsSuccessfulActivated, Is.False);
			Assert.That(manager.MessageBoxes.Implode(), Is.EqualTo("Нет предложений"));
		}

		private void CleanSendOrders(Offer offer)
		{
			session.Query<SentOrderLine>()
				.Where(l => l.CatalogId == offer.CatalogId)
				.Each(l => session.Delete(l));
			session.Flush();
		}
	}
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Resources;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;
using Common.Tools;
using Microsoft.Reactive.Testing;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI;
using ReactiveUI.Testing;
using AnalitF.Net.Client.Helpers;

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
				.First(c => session.Query<Offer>().Count(o => o.CatalogId == c.Id) >= 2);
		}

		[Test]
		public void Filter_by_producer()
		{
			Assert.That(model.Producers.Count, Is.EqualTo(3));
			model.CurrentProducer = model.Producers[1];
			Assert.That(model.Offers.Count, Is.EqualTo(1));
		}

		[Test]
		public void Calculate_retail_cost()
		{
			var splitCost = model.Offers[0].Cost;
			var markupConfig1 = new MarkupConfig(0, splitCost, 20);
			var markupConfig2 = new MarkupConfig(splitCost, 100 * splitCost, 30);
			session.DeleteEach(session.Query<MarkupConfig>());
			session.Save(markupConfig1);
			session.Save(markupConfig2);

			Assert.That(model.Offers[0].RetailCost, Is.Not.EqualTo(0));

			model.CurrentOffer = model.Offers[0];
			Assert.That(model.RetailMarkup, Is.EqualTo(20));
			Assert.That(model.RetailCost, Is.EqualTo(model.Offers[0].Cost * (decimal)1.2));
			model.CurrentOffer = model.Offers[1];
			Assert.That(model.RetailMarkup, Is.EqualTo(30));
			Assert.That(model.RetailCost, Is.EqualTo(model.Offers[1].Cost * (decimal)1.3));
			model.RetailMarkup = 23;
			model.CurrentOffer = model.Offers[0];
			Assert.That(model.RetailMarkup, Is.EqualTo(23));
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
			MakeDifferentCategory();

			Assert.That(model.CurrentOffer.Id, Is.EqualTo(model.Offers[1].Id));
		}

		[Test]
		public void Filter_by_price_category()
		{
			MakeDifferentCategory();

			model.CurrentFilter = model.Filters[1];
			Assert.That(model.Offers.Count, Is.EqualTo(1));
			Assert.That(model.Offers[0].Price.BasePrice, Is.True);

			model.CurrentFilter = model.Filters[0];
			Assert.That(model.Offers.Count, Is.EqualTo(2));

			model.CurrentFilter = model.Filters[2];
			Assert.That(model.Offers.Count, Is.EqualTo(1));
			Assert.That(model.Offers[0].Price.BasePrice, Is.False, model.Offers[0].Price.Id.ToString());
		}

		[Test]
		public void Filter_result_empty()
		{
			var ids = model.Offers.Select(o => o.Price.Id).Distinct().ToList();
			var prices = session.Query<Price>().Where(p => ids.Contains(p.Id)).ToList();

			foreach (var price in prices)
				price.BasePrice = true;
			session.Flush();

			model.CurrentFilter = model.Filters[2];
		}

		[Test]
		public void Load_max_producer_costs()
		{
			//предельные цены формируются только для прайса которые был создан в текущем импорте
			//и для прайсов созданых в предыдущих импортах он не будет сформирован
			var currentPrice = session.Query<Price>().Select(p => p.Id).ToArray().Max();
			var catalogId = session.Query<Offer>().First(o => o.Price.Id == currentPrice && o.VitallyImportant).CatalogId;
			catalog = session.Load<Catalog>(catalogId);

			Assert.That(model.MaxProducerCosts.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Load_history_orders()
		{
			var offer = model.Offers.First();

			CleanSendOrders(offer);

			var order = new Order(offer.Price, address);
			order.AddLine(offer, 1);
			var sentOrder = new SentOrder(order);
			session.Save(sentOrder);
			session.Flush();

			schedule.AdvanceToMs(4000);
			Assert.That(model.HistoryOrders.Count, Is.EqualTo(1));
			Assert.That(model.HistoryOrders[0].Count, Is.EqualTo(1));
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
			model.GroupByProduct = true;
			Assert.That(model.Offers.Select(o => o.Cost).Implode(), Is.EqualTo("90, 120, 103, 105"));
		}

		[Test]
		public void Load_order_history()
		{
			session.DeleteEach(session.Query<SentOrderLine>());

			CleanSendOrders(model.CurrentOffer);
			model.LoadHistoryOrders();
		}

		[Test]
		public void Load_order_history_without_address()
		{
			session.DeleteEach(session.Query<Order>());
			session.DeleteEach(session.Query<SentOrder>());
			session.DeleteEach(session.Query<Address>());
			model.LoadHistoryOrders();
		}

		[Test, RequiresSTA]
		public void Export()
		{
			Assert.That(model.CanExport, Is.True);
			var app = new Client.App();
			System.Windows.Application.LoadComponent(app, new Uri("/AnalitF.Net.Client;component/app.xaml", UriKind.Relative));
			((IViewAware)model).AttachView(new CatalogOfferView());
			var result = (OpenFileResult)model.Export();
			Assert.That(File.Exists(result.Filename), result.Filename);
			File.Delete(result.Filename);
		}

		[Test, Ignore]
		public void Print()
		{
			Assert.That(model.CanPrint, Is.True);
			model.Print();
		}

		private void CleanSendOrders(Offer offer)
		{
			session.Query<SentOrderLine>()
				.Where(l => l.CatalogId == offer.CatalogId)
				.Each(l => session.Delete(l));
		}

		private void MakeDifferentCategory()
		{
			var offers = session.Query<Offer>().Where(o => o.CatalogId == catalog.Id).ToList();
			Assert.That(offers[0].Price.Id, Is.Not.EqualTo(offers[1].Price.Id));

			var price1 = session.Load<Price>(offers[0].Price.Id);
			var price2 = session.Load<Price>(offers[1].Price.Id);
			price1.BasePrice = false;
			price2.BasePrice = true;
			session.Save(price1);
			session.Save(price2);
			session.Flush();
		}
	}
}
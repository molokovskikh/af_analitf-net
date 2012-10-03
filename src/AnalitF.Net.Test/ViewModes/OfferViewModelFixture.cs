﻿using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.ViewModes
{
	[TestFixture]
	public class OfferViewModelFixture : BaseFixture
	{
		private OfferViewModel model;
		private ISession session;

		[SetUp]
		public void Setup()
		{
			session = Client.Config.Initializers.NHibernate.Factory.OpenSession();
			var catalog = session.Query<Catalog>()
				.Where(c => session.Query<Offer>().Count(o => o.CatalogId == c.Id) >= 2)
				.First();
			model = new OfferViewModel(catalog);
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
			using(var t = session.BeginTransaction()) {
				session.Save(markupConfig1);
				session.Save(markupConfig2);
				t.Commit();
			}
			model = new OfferViewModel(model.CurrentCatalog);

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
	}
}
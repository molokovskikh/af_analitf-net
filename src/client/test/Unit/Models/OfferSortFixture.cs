﻿using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Offers;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit.Models
{
	[TestFixture]
	public class OfferSortFixture
	{
		private List<Offer> offers;

		[SetUp]
		public void Setup()
		{
			offers = new List<Offer> {
				new Offer {
					ProductId = 50,
					Cost = 30
				},
				new Offer {
					ProductId = 51,
					Cost = 34
				},
				new Offer {
					ProductId = 52,
					Cost = 90
				},
				new Offer {
					ProductId = 51,
					Cost = 33
				},
			};
		}

		[Test]
		public void After_sort_assign_product_index()
		{
			offers = BaseOfferViewModel.SortByMinCostInGroup(offers, o => o.ProductId);
			Assert.IsFalse(offers[0].IsGrouped);
			Assert.IsTrue(offers[1].IsGrouped);
			Assert.IsFalse(offers[3].IsGrouped);
		}

		[Test]
		public void Sort_offer_by_min_cost()
		{
			offers = new List<Offer> {
				new Offer {
					ProductId = 2,
					Cost = 54.3m,
				},
				new Offer {
					ProductId = 1,
					Cost = 54.3m
				},
				new Offer {
					ProductId = 2,
					Cost = 54.3m
				},
			};
			offers = BaseOfferViewModel.SortByMinCostInGroup(offers, o => o.ProductId);
			Assert.AreEqual(1, offers[0].ProductId);
			Assert.AreEqual(2, offers[1].ProductId);
			Assert.AreEqual(2, offers[2].ProductId);
		}

		[Test]
		public void Sort_by_product()
		{
			offers = new List<Offer> {
				new Offer {
					ProductId = 1,
					Cost = 53.38m,
				},
				new Offer {
					ProductId = 2,
					Cost = 53.38m
				},
				new Offer {
					ProductId = 2,
					Cost = 54.14m
				},
				new Offer {
					ProductId = 1,
					Cost = 54.14m
				},
			};
			offers = BaseOfferViewModel.SortByMinCostInGroup(offers, o => o.ProductId);
			Assert.AreEqual(1, offers[0].ProductId);
			Assert.AreEqual(1, offers[1].ProductId);
			Assert.AreEqual(2, offers[2].ProductId);
			Assert.AreEqual(2, offers[3].ProductId);
		}

		[Test]
		public void Group_by_product()
		{
			//5 разных товаров но цена у всех одинаковая, группы должны выделяться правильно
			offers = new List<Offer> {
				new Offer {
					ProductId = 1,
					Cost = 53.38m,
				},
				new Offer {
					ProductId = 2,
					Cost = 53.38m
				},
				new Offer {
					ProductId = 2,
					Cost = 54.14m
				},
				new Offer {
					ProductId = 1,
					Cost = 54.14m
				},
				new Offer {
					ProductId = 3,
					Cost = 53.38m
				},
				new Offer {
					ProductId = 4,
					Cost = 53.38m
				},
				new Offer {
					ProductId = 5,
					Cost = 53.38m
				},
				new Offer {
					ProductId = 6,
					Cost = 53.38m
				},
			};
			offers = BaseOfferViewModel.SortByMinCostInGroup(offers.OrderBy(o => Guid.NewGuid()).ToList(), o => o.ProductId);
			Assert.AreEqual(1, offers[0].ProductId);
			Assert.IsFalse(offers[0].IsGrouped);
			Assert.AreEqual(1, offers[1].ProductId);
			Assert.IsFalse(offers[1].IsGrouped);

			Assert.AreEqual(2, offers[2].ProductId);
			Assert.IsTrue(offers[2].IsGrouped);
			Assert.AreEqual(2, offers[3].ProductId);

			Assert.IsFalse(offers[4].IsGrouped);
			Assert.IsTrue(offers[5].IsGrouped);
			Assert.IsFalse(offers[6].IsGrouped);
			Assert.IsTrue(offers[7].IsGrouped);
		}

		[Test]
		public void Reset_group_key()
		{
			offers = BaseOfferViewModel.SortByMinCostInGroup(offers, o => o.ProductId);
			Assert.IsTrue(offers[1].IsGrouped);
			offers = BaseOfferViewModel.SortByMinCostInGroup(offers, o => o.CatalogId, false);
			Assert.IsFalse(offers[1].IsGrouped);
		}
	}
}
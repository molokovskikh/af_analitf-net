﻿using System.Collections.Generic;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class OfferSortFixture
	{
		[Test]
		public void After_sort_assign_product_index()
		{
			var offers = new List<Offer> {
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

			offers = BaseOfferViewModel.SortByMinCostInGroup(offers, o => o.ProductId);
			Assert.That(offers[0].SortKeyGroup, Is.EqualTo(0));
			Assert.That(offers[1].SortKeyGroup, Is.EqualTo(1));
			Assert.That(offers[3].SortKeyGroup, Is.EqualTo(0));
		}
	}
}
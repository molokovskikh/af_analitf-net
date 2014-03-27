using System.Collections.Generic;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
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
		public void Reset_group_key()
		{
			offers = BaseOfferViewModel.SortByMinCostInGroup(offers, o => o.ProductId);
			Assert.IsTrue(offers[1].IsGrouped);
			offers = BaseOfferViewModel.SortByMinCostInGroup(offers, o => o.CatalogId, false);
			Assert.IsFalse(offers[1].IsGrouped);
		}
	}
}
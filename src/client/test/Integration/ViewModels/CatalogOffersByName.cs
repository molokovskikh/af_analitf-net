﻿using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Offers;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class CatalogOffersByName : ViewModelFixture
	{
		[Test]
		public void Show_grouped_offers()
		{
			var catalog = session.Query<Catalog>()
				.First(c => c.HaveOffers && session.Query<Offer>().Count(o => o.CatalogId == c.Id) >= 2);

			var model = Open(new CatalogOfferViewModel(catalog.Name));
			Assert.That(model.IsFilterByCatalogName, Is.True);
			Assert.That(model.Offers.Value[0].GroupName, Is.Not.Null);
		}
	}
}
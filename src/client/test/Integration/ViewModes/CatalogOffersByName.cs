using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Offers;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class CatalogOffersByName : ViewModelFixture
	{
		[Test]
		public void Show_grouped_offers()
		{
			var catalog = session.Query<Catalog>()
				.First(c => c.HaveOffers && session.Query<Offer>().Count(o => o.CatalogId == c.Id) >= 2);

			var model = Init(new CatalogOfferViewModel(catalog.Name));
			Assert.That(model.IsFilterByCatalogName, Is.True);
			Assert.That(model.Offers.Value[0].GroupName, Is.Not.Null);
		}
	}
}
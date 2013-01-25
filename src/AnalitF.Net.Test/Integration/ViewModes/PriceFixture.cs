using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class PriceFixture : BaseFixture
	{
		[Test]
		public void Load_order()
		{
			var offer = session.Query<Offer>().First();
			MakeOrder(offer);

			var model = Init(new PriceViewModel());
			Assert.That(model.Prices.First(p => p.Id == offer.Price.Id).Order, Is.Not.Null);
			Assert.That(model.Prices[0].MinOrderSum, Is.Not.Null);
		}
	}
}
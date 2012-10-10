using AnalitF.Net.Client.Models;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class OfferFixture
	{
		[Test]
		public void Update_order_count()
		{
			var offer = new Offer {
				Price = new Price(),
				Cost = 53.1m
			};
			offer.OrderCount = 10;
			Assert.That(offer.OrderSum, Is.EqualTo(531));
			offer.UpdateOrderLine();
			Assert.That(offer.OrderLine, Is.Not.Null);
			Assert.That(offer.Price.Order, Is.Not.Null);
			Assert.That(offer.OrderLine.Count, Is.EqualTo(10));
		}
	}
}
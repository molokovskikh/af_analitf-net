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

		[Test]
		public void Junk_warning()
		{
			var offer = new Offer {
				Price = new Price(),
				Junk = true
			};
			offer.OrderCount = 1;
			offer.UpdateOrderLine();
			Assert.That(offer.Warning, Is.StringContaining("Вы заказали препарат с ограниченным сроком годности"));
		}

		[Test]
		public void Reset_message_on_delete()
		{
			var offer = new Offer {
				Price = new Price(),
				Junk = true
			};
			offer.OrderCount = 1;
			offer.UpdateOrderLine();
			Assert.That(offer.Warning, Is.Not.Null);
			offer.OrderCount = 0;
			offer.UpdateOrderLine();
			Assert.That(offer.Warning, Is.Null);
		}
	}
}
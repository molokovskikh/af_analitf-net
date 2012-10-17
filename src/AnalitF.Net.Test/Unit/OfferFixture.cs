using AnalitF.Net.Client.Models;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class OfferFixture
	{
		private Offer offer;

		[SetUp]
		public void Setup()
		{
			offer = new Offer {
				Price = new Price(),
				Cost = 53.1m
			};
		}

		[Test]
		public void Update_order_count()
		{
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
			offer.Junk = true;
			offer.OrderCount = 1;
			offer.MakePreorderCheck();
			Assert.That(offer.Warning, Is.StringContaining("Вы заказали препарат с ограниченным сроком годности"));
		}

		[Test]
		public void Reset_message_on_delete()
		{
			offer.Junk = true;
			offer.OrderCount = 1;
			offer.MakePreorderCheck();
			Assert.That(offer.Warning, Is.Not.Null);
			offer.OrderCount = 0;
			offer.MakePreorderCheck();
			var order = offer.UpdateOrderLine();
			Assert.That(offer.Warning, Is.Null);
			Assert.That(order, Is.Null);
		}

		[Test]
		public void Can_not_order_to_many()
		{
			offer.OrderCount = uint.MaxValue;
			offer.MakePreorderCheck();
			Assert.That(offer.OrderCount, Is.EqualTo(65535));
		}

		[Test]
		public void Reject_order_more_than_in_storage()
		{
			offer.Quantity = "10";
			offer.OrderCount = 15;
			offer.MakePreorderCheck();
			Assert.That(offer.OrderCount, Is.EqualTo(10));
			Assert.That(offer.Notification, Is.EqualTo("Заказ превышает остаток на складе, товар будет заказан в количестве 10"));
		}

		[Test]
		public void Check_order_rules()
		{
			offer.Quantity = "23";
			offer.OrderCount = 50;
			offer.RequestRatio = 5;
			offer.UpdateOrderLine();
			Assert.That(offer.OrderCount, Is.EqualTo(20));
			Assert.That(offer.OrderLine.Count, Is.EqualTo(20));
		}

		[Test]
		public void Check_min_order_sum()
		{
			offer.OrderCount = 1;
			offer.MinOrderSum = 100;
			offer.Cost = 70;
			offer.UpdateOrderLine();
			Assert.That(offer.Notification, Is.EqualTo("Сумма заказа \"70\" меньше минимальной сумме заказа \"100\" по данной позиции!"));
			Assert.That(offer.OrderCount, Is.EqualTo(0));
			Assert.That(offer.OrderLine, Is.Null);
			Assert.That(offer.Price.Order, Is.Null);
		}

		[Test]
		public void Delete_order_line()
		{
			offer.OrderCount = 1;
			offer.UpdateOrderLine();
			offer.OrderCount = 0;
			var order = offer.UpdateOrderLine();
			Assert.That(order, Is.Not.Null);
		}

		[Test]
		public void Calculate_correct_order_only_if_over_order()
		{
			offer.RequestRatio = 3;
			offer.Quantity = "10";
			offer.OrderCount = 1;
			offer.MakePreorderCheck();
			Assert.That(offer.OrderCount, Is.EqualTo(1));
			Assert.That(offer.Notification, Is.Null);
		}
	}
}
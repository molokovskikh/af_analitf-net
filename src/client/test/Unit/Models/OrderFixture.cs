using System.Text;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit.Models
{
	[TestFixture]
	public class OrderFixture
	{
		private Price price;
		private Address address;
		private Offer offer;

		[SetUp]
		public void Setup()
		{
			price = new Price("test1");
			address = new Address();
			offer = new Offer(price, 100) {
				ProductId = 1
			};
		}

		[Test]
		public void Update_sum_order_add_line()
		{
			var order = new Order(price, address);
			offer.Cost = 10;
			order.TryOrder(offer, 20);
			Assert.That(order.Sum, Is.EqualTo(200));
			Assert.That(order.LinesCount, Is.EqualTo(1));
		}

		[Test]
		public void Calculate_is_valid()
		{
			var order = new Order(price, address);
			order.TryOrder(offer, 1);
			Assert.That(order.IsInvalid, Is.True);

			order.Address.Rules.Add(new MinOrderSumRule(order.Address, order.Price, 1000));
			order = new Order(price, address);
			order.TryOrder(offer, 1);
			Assert.IsTrue(order.IsInvalid);
		}

		[Test]
		public void Merge_order_line()
		{
			var order = new Order(price, address);
			order.TryOrder(offer, 1);

			var frozen = new Order(price, address) {
				Frozen = true,
				Send = false
			};
			frozen.TryOrder(offer, 1);
			frozen.Lines[0].Merge(order, new[] { offer }, new StringBuilder());

			Assert.That(order.Lines[0].Count, Is.EqualTo(2));
			Assert.That(frozen.Lines.Count, Is.EqualTo(0));
		}

		[Test]
		public void Restore_order_line()
		{
			var order = new Order(price, address);
			var frozen = new Order(price, address) {
				Frozen = true,
				Send = false
			};
			frozen.TryOrder(offer, 1);
			offer.Note = "test";
			frozen.Lines[0].Merge(order, new[] { offer }, new StringBuilder());
			Assert.That(order.Lines[0].Note, Is.EqualTo("test"));
		}

		[Test]
		public void Remove_unrestorable_line()
		{
			var order = new Order(price, address);
			var frozen = new Order(price, address) {
				Frozen = true,
				Send = false
			};
			offer.Quantity = "5";
			frozen.TryOrder(offer, 1);
			offer.RequestRatio = 7;
			frozen.Lines[0].Merge(order, new[] { offer }, new StringBuilder());
			Assert.That(frozen.Lines.Count, Is.EqualTo(0));
			Assert.That(order.Lines.Count, Is.EqualTo(0));
		}

		[Test]
		public void Split_restore_line()
		{
			var frozen = new Order(price, address) {
				Frozen = true,
				Send = false
			};
			var offer1 = new Offer(offer, 150) {
				Quantity = "1000"
			};
			frozen.TryOrder(offer, 100);
			offer.Quantity = "20";
			var order = new Order(price, address);
			frozen.Lines[0].Merge(order, new[] { offer, offer1 }, new StringBuilder());
			Assert.That(order.Lines.Count, Is.EqualTo(2));
			Assert.That(order.Lines[0].Count, Is.EqualTo(20));
			Assert.That(order.Lines[1].Count, Is.EqualTo(80));
		}

		[Test]
		public void Reorder()
		{
			var price1 = new Price("test2");
			var price2 = new Price("test3");
			var order = new Order(price, address);
			order.TryOrder(offer, 10);
			var orders = new[] {
				new Order(price1, address),
				new Order(price2, address)
			};
			var offer1 = new Offer(price1, 100) { ProductId = 1 };
			orders[0].TryOrder(offer1, 10);
			var offer2 = new Offer(price2, 150) { ProductId = 1 };
			orders[1].TryOrder(offer2, 1);

			var log = new StringBuilder();
			ReorderCommand<Order>.Reorder(order, orders, new[] { offer1, offer2 }, log);
			Assert.That(order.Lines.Count, Is.EqualTo(0));
			Assert.That(orders[0].Lines.Count, Is.EqualTo(1));
			Assert.That(orders[0].Lines[0].Count, Is.EqualTo(20));
		}

		[Test]
		public void Process_server_result()
		{
			var order = new Order(price, address);
			order.TryOrder(offer, 10);
			order.Apply(new OrderResult());
			Assert.AreEqual(OrderResultStatus.OK, order.SendResult);
		}

		[Test]
		public void Remove_line()
		{
			var order = new Order(price, address);
			var line = order.TryOrder(offer, 10);
			Assert.AreEqual(1, order.LinesCount);
			order.RemoveLine(line);
			Assert.AreEqual(0, order.LinesCount);
		}
	}
}
using AnalitF.Net.Client.Models;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class OrderFixture
	{
		[Test]
		public void Update_sum_order_add_line()
		{
			var order = new Order(new Price(), new Address());
			order.AddLine(new Offer { Cost = 10 }, 20);
			Assert.That(order.Sum, Is.EqualTo(200));
			Assert.That(order.LinesCount, Is.EqualTo(1));
		}

		[Test]
		public void Calculate_is_valid()
		{
			var order = new Order(new Price(), new Address());
			order.AddLine(new Offer { Cost = 10 }, 20);
			Assert.That(order.IsValid, Is.True);
			order.Address.Rules.Add(new MinOrderSumRule(order.Address, order.Price, 1000));
			Assert.That(order.IsValid, Is.False);
		}
	}
}
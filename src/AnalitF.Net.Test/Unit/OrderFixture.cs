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
	}
}
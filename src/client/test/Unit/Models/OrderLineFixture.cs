using System.Linq;
using AnalitF.Net.Client.Models;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit.Models
{
	[TestFixture]
	public class OrderLineFixture
	{
		private OrderLine line;
		private Order order;

		[SetUp]
		public void Setup()
		{
			var price = new Price("АМП (Основной)");
			order = new Order(price, new Address("Тестовый адрес"));
			var offer = new Offer(price, 100) {
				ProductSynonym = "ЭХИНАЦЕЯ ТРАВА пачка 50г (18%)",
				ProducerSynonym = "Камелия-ЛТ ООО",
			};
			line = order.TryOrder(offer, 1);

			line.Apply(new OrderLineResult {
				ServerCost = 150,
				Result = LineResultStatus.CostChanged
			});
		}

		[Test]
		public void Mark_cost()
		{
			Assert.IsTrue(line.IsCostChanged);
			Assert.IsTrue(line.IsCostIncreased);
			Assert.IsFalse(line.IsCostDecreased);
			Assert.AreEqual(100, line.OldCost);
			Assert.AreEqual(150, line.NewCost);
		}

		[Test]
		public void Reset_status()
		{
			order.Frozen = true;
			Assert.AreEqual("", line.LongSendError);
			Assert.AreEqual(LineResultStatus.OK, line.SendResult);
			Assert.IsNull(line.NewCost);
			Assert.IsNull(line.OldCost);
		}

		[Test]
		public void Report_by_address()
		{
			var report = OrderLine.SendReport(order.Lines, true);
			Assert.AreEqual("адрес доставки Тестовый адрес\r\n" +
				"    прайс-лист АМП (Основной)\r\n" +
				"        ЭХИНАЦЕЯ ТРАВА пачка 50г (18%) - Камелия-ЛТ ООО: имеется различие в цене препарата" +
				" (старая цена: 100,00р.; новая цена: 150,00р.)\r\n", report);
		}

		[Test]
		public void Report()
		{
			var report = OrderLine.SendReport(order.Lines, false);
			Assert.AreEqual("прайс-лист АМП (Основной)\r\n" +
				"    ЭХИНАЦЕЯ ТРАВА пачка 50г (18%) - Камелия-ЛТ ООО: имеется различие в цене препарата" +
				" (старая цена: 100,00р.; новая цена: 150,00р.)\r\n", report);
		}

		[Test]
		public void Calculate_mixed_cost()
		{
			var user = new User();
			line.Order.Price.CostFactor = 1.5m;
			line.CalculateRetailCost(Enumerable.Empty<MarkupConfig>(), user);
			Assert.AreEqual(100, line.MixedCost);
			Assert.AreEqual(100, line.MixedSum);

			user.IsDeplayOfPaymentEnabled = true;
			line.CalculateRetailCost(Enumerable.Empty<MarkupConfig>(), user);
			Assert.AreEqual(150, line.MixedCost);
			Assert.AreEqual(150, line.MixedSum);
		}
	}
}
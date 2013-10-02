﻿using AnalitF.Net.Client.Models;
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
			var price = new Price {
				Id = new PriceComposedId(),
				Name = "АМП (Основной)"
			};

			order = new Order(price, new Address());
			var offer = new Offer(price, 100) {
				ProductSynonym = "ЭХИНАЦЕЯ ТРАВА пачка 50г (18%)",
				ProducerSynonym = "Камелия-ЛТ ООО",
			};
			line = order.AddLine(offer, 1);

			line.Apply(new OrderLineResult {
				ServerCost = 150,
				Result = LineResultStatus.CostChanged
			});
		}

		[Test]
		public void Mark_cost()
		{
			Assert.IsTrue(line.IsCostChanged);
			Assert.AreEqual(100, line.OldCost);
			Assert.AreEqual(150, line.NewCost);
		}

		[Test]
		public void Report()
		{
			var report = OrderLine.SendReport(order.Lines);
			Assert.AreEqual("прайс-лист АМП (Основной)\r\n" +
				"    ЭХИНАЦЕЯ ТРАВА пачка 50г (18%) - Камелия-ЛТ ООО: имеется различие в цене препарата" +
				" (старая цена: 100,00р.; новая цена: 150,00р.)\r\n", report);
		}
	}
}
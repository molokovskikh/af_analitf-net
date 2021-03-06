﻿using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit.Models
{
	[TestFixture]
	public class OrderLineFixture
	{
		private OrderLine line;
		private Order order;
		private Price price;
		private User user;
		private Settings settings;

		[SetUp]
		public void Setup()
		{
			price = new Price("АМП (Основной)");
			order = new Order(price, new Address("Тестовый адрес"));
			user = new User();
			settings = new Settings(order.Address);
			settings.Waybills.Add(new WaybillSettings(user, order.Address));
			var offer = new Offer(price, 100) {
				Settings = settings,
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
		public void Reset_status_before_apply_new()
		{
			line.Apply(new OrderLineResult {
				Result = LineResultStatus.NoOffers
			});
			Assert.IsNull(line.NewCost);
			Assert.IsNull(line.OldCost);
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
				$" (старая цена: {100.00:C}; новая цена: {150.00:C})\r\n", report);
		}

		[Test]
		public void Report()
		{
			var report = OrderLine.SendReport(order.Lines, false);
			Assert.AreEqual("прайс-лист АМП (Основной)\r\n" +
				"    ЭХИНАЦЕЯ ТРАВА пачка 50г (18%) - Камелия-ЛТ ООО: имеется различие в цене препарата" +
				$" (старая цена: {100.00:C}; новая цена: {150.00:C})\r\n", report);
		}

		[Test]
		public void Format_quantity()
		{
			order = new Order(price, new Address("Тестовый адрес"));
			var offer = new Offer(price, 100) {
				ProductSynonym = "ЭХИНАЦЕЯ ТРАВА пачка 50г (18%)",
				ProducerSynonym = "Камелия-ЛТ ООО",
			};
			line = order.TryOrder(offer, 10);
			line.SendResult |= LineResultStatus.QuantityChanged;
			line.NewQuantity = 1;
			line.OldQuantity = 10;
			line.HumanizeSendError();

			var expected = @"ЭХИНАЦЕЯ ТРАВА пачка 50г (18%) - Камелия-ЛТ ООО: доступное количество препарата в прайс-листе меньше заказанного ранее (старый заказ: 10; текущий заказ: 1)";
			Assert.AreEqual(expected, line.ToString("r", null));
		}

		[Test]
		public void Calculate_mixed_cost()
		{
			line.Order.Price.CostFactor = 1.5m;
			line.CalculateRetailCost(settings.Markups, new List<uint>(), user);
			Assert.AreEqual(100, line.MixedCost);
			Assert.AreEqual(100, line.MixedSum);

			user.IsDelayOfPaymentEnabled = true;
			line.CalculateRetailCost(settings.Markups, new List<uint>(), user);
			Assert.AreEqual(150, line.MixedCost);
			Assert.AreEqual(150, line.MixedSum);
		}
	}
}
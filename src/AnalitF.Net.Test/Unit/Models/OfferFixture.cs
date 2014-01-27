﻿using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using Address = AnalitF.Net.Client.Models.Address;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class OfferFixture
	{
		private Offer offer;
		private Address address;

		private string error;
		private string warning;
		private Settings settings;

		[SetUp]
		public void Setup()
		{
			settings = new Settings();
			address = new Address();
			offer = new Offer {
				Price = new Price(),
				Cost = 53.1m
			};
			error = null;
			warning = null;
		}

		[Test]
		public void Update_order_count()
		{
			offer.OrderCount = 10;
			offer.UpdateOrderLine(address, settings);
			Assert.That(offer.OrderLine, Is.Not.Null);
			Assert.That(offer.Price.Order, Is.Not.Null);
			Assert.That(offer.OrderLine.Count, Is.EqualTo(10));
			Assert.That(offer.OrderLine.Sum, Is.EqualTo(531));
			Assert.That(offer.OrderLine.Order.Sum, Is.EqualTo(531));
			Assert.That(offer.OrderLine.Order.LinesCount, Is.EqualTo(1));
		}

		[Test]
		public void Update_count()
		{
			offer.OrderCount = 10;
			offer.UpdateOrderLine(address, settings);
			offer.OrderCount = 5;
			offer.UpdateOrderLine(address, settings);
			Assert.That(offer.OrderCount, Is.EqualTo(5));
			Assert.That(offer.OrderLine.Count, Is.EqualTo(5));
			Assert.That(offer.OrderLine.Order.Sum, Is.EqualTo(265.5));
		}

		[Test]
		public void Junk_warning()
		{
			offer.Junk = true;
			offer.OrderCount = 1;
			Validate();
			Assert.That(warning, Is.StringContaining("Вы заказали препарат с ограниченным сроком годности"));
		}

		[Test]
		public void Reset_message_on_delete()
		{
			offer.Junk = true;
			offer.OrderCount = 1;
			Validate();
			Assert.That(warning, Is.Not.Null);
			offer.OrderCount = 0;
			Validate();
			Read(offer.SaveOrderLine(address, settings));
			Assert.That(warning, Is.Null.Or.Empty);
			Assert.That(offer.Price.Order, Is.Null);
			Assert.That(offer.OrderLine, Is.Null);
		}

		[Test]
		public void Can_not_order_to_many()
		{
			offer.OrderCount = uint.MaxValue;
			Validate();
			Assert.That(offer.OrderCount, Is.EqualTo(65535));
		}

		[Test]
		public void Reject_order_more_than_in_storage()
		{
			offer.Quantity = "10";
			offer.OrderCount = 15;
			Validate();
			Assert.That(offer.OrderCount, Is.EqualTo(10));
			Assert.That(error, Is.EqualTo("Заказ превышает остаток на складе, товар будет заказан в количестве 10"));
		}

		[Test]
		public void Check_order_rules()
		{
			offer.Quantity = "23";
			offer.OrderCount = 50;
			offer.RequestRatio = 5;
			offer.UpdateOrderLine(address, settings);
			Assert.That(offer.OrderCount, Is.EqualTo(20));
			Assert.That(offer.OrderLine.Count, Is.EqualTo(20));
		}

		[Test]
		public void Check_min_order_sum()
		{
			offer.OrderCount = 1;
			offer.MinOrderSum = 100;
			offer.Cost = 70;
			offer.UpdateOrderLine(address, settings);
			Read(offer.SaveOrderLine(address, settings));
			Assert.That(error, Is.EqualTo("Сумма заказа \"70\" меньше минимальной сумме заказа \"100\" по данной позиции!"));
			Assert.That(offer.OrderCount, Is.Null);
			Assert.That(offer.OrderLine, Is.Null);
			Assert.That(offer.Price.Order, Is.Null);
		}

		[Test]
		public void Delete_order_line()
		{
			offer.OrderCount = 1;
			offer.UpdateOrderLine(address, settings);
			offer.OrderCount = 0;
			offer.UpdateOrderLine(address, settings);
			Assert.That(offer.OrderCount, Is.Null);
			Assert.That(offer.OrderLine, Is.Null);
			Assert.That(offer.Price.Order, Is.Null);
		}

		[Test]
		public void Calculate_correct_order_only_if_over_order()
		{
			offer.RequestRatio = 3;
			offer.Quantity = "10";
			offer.OrderCount = 1;
			Validate();
			Assert.That(offer.OrderCount, Is.EqualTo(1));
			Assert.That(error, Is.Null.Or.Empty);
		}

		[Test]
		public void Calculate_markup()
		{
			offer.ProducerCost = 10;
			offer.Cost = 12.1m;
			Assert.That(offer.SupplierMarkup, Is.EqualTo(10));
		}

		[Test]
		public void Accamulate_warnings()
		{
			offer.Junk = true;
			offer.Quantity = "10000";
			offer.OrderCount = 1001;
			Validate();
			Assert.That(warning, Is.EqualTo("Внимание! Вы заказали большое количество препарата."
				+ "\r\nВы заказали препарат с ограниченным сроком годности"
				+ "\r\nили с повреждением вторичной упаковки."));
		}

		[Test]
		public void Can_not_order_without_address()
		{
			address = null;
			offer.OrderCount = 1;
			Validate();
			Assert.That(offer.OrderCount, Is.Null);
		}

		[Test]
		public void Remove_order_on_order_line_delete()
		{
			offer.OrderCount = 1;
			Validate();
			Assert.That(address.Orders.Count, Is.EqualTo(1));
			offer.OrderCount = 0;
			Validate();
			Assert.That(address.Orders.Count, Is.EqualTo(0));
		}

		[Test]
		public void Validate_avg_cost()
		{
			offer.PrevOrderAvgCount = 10;
			offer.OrderCount = 51;
			Validate();
			Assert.That(warning, Is.EqualTo("Превышение среднего заказа!"));
		}

		[Test]
		public void Warning_if_exist_in_frozen()
		{
			offer.OrderCount = 1;
			offer.UpdateOrderLine(address, settings);
			offer.SaveOrderLine(address, settings);
			address.Orders[0].Frozen = true;

			offer.OrderLine = null;
			offer.OrderCount = null;
			offer.OrderCount = 1;
			Validate();
			Assert.That(warning, Is.EqualTo("Товар присутствует в замороженных заказах."));
		}

		[Test]
		public void Update_order_line()
		{
			offer.OrderCount = 1;
			offer.UpdateOrderLine(address, settings);

			offer.OrderLine = null;
			Assert.That(offer.OrderCount, Is.Null);
		}

		[Test]
		public void Invalid_supplier_markup()
		{
			offer.ProducerCost = 0;
			Assert.IsNull(offer.SupplierMarkup);
		}

		[Test]
		public void Attach_order_line()
		{
			var order = new Order(address, offer);
			offer.OrderLine = order.Lines[0];
			Assert.AreEqual(offer.Price.Order, order);
			Assert.AreEqual(offer.OrderCount, 1);

			offer.OrderLine = null;
			Assert.IsNull(offer.OrderCount);
		}

		[Test]
		public void Delay_of_payment()
		{
			offer.Cost = 100;
			Assert.AreEqual(100, offer.ResultCost);

			offer.Price.DelayOfPayments.Add(new DelayOfPayment(50));
			Assert.AreEqual(offer.ResultCost, 150);
		}

		[Test]
		public void Delay_of_payment_per_day()
		{
			offer.Price.DelayOfPayments.Add(new DelayOfPayment(DateTime.Today.AddDays(-1).DayOfWeek, 10));
			offer.Price.DelayOfPayments.Add(new DelayOfPayment(DateTime.Today.DayOfWeek, 20));
			offer.Cost = 100;
			Assert.AreEqual(120, offer.ResultCost);
		}

		[Test]
		public void Actual_leader_price()
		{
			offer.LeaderPrice = new Price("Тест1");
			offer.LeaderCost = offer.Cost;
			Assert.AreEqual(offer.ResultLeaderPrice, offer.Price);
		}

		private void Validate()
		{
			var message = offer.UpdateOrderLine(address, settings);
			Read(message);
		}

		private void Read(List<Message> message)
		{
			warning = message.Where(m => m.IsWarning).Implode(Environment.NewLine);
			error = message.Where(m => m.IsError).Implode(Environment.NewLine);
		}
	}
}
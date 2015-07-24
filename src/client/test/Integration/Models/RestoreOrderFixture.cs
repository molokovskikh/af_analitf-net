using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.NHibernate;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.Models
{
	[TestFixture]
	public class RestoreOrderFixture : DbFixture
	{
		[Test]
		public void Restore_order_items()
		{
			session.DeleteEach<Order>();

			var offer = session.Query<Offer>().First(o => !session.Query<Offer>().Any(x => x.Price == o.Price
				&& x.ProductId == o.ProductId && x.Id.OfferId != o.Id.OfferId));

			var order = new Order(address, offer);
			session.Save(order);
			var oldCost = offer.Cost;
			offer.Cost = 1000m * (decimal)Generator.RandomDouble().First();
			session.Save(offer);

			order.Frozen = true;
			var cmd = InitCmd(new UnfreezeCommand<Order>(order.Id) {
				Restore = true
			});
			cmd.Execute();

			Assert.AreEqual(String.Format(@"адрес доставки {0}
    прайс-лист {1}
        {2} - {3}: имеется различие в цене препарата (старая цена: {4:C}; новая цена: {5:C})
", address.Name, offer.Price.Name, offer.ProductSynonym, offer.ProducerSynonym, oldCost, offer.Cost), cmd.Result);
			var orders = session.Query<Order>().ToArray();
			Assert.AreEqual(1, orders.Length);
			order = orders[0];
			var line = order.Lines[0];
			Assert.AreEqual(LineResultStatus.CostChanged, line.SendResult);
			var message = String.Format("имеется различие в цене препарата (старая цена: {0:C}; новая цена: {1:C})",
				oldCost, offer.Cost);
			Assert.AreEqual(message, line.LongSendError);
		}

		[Test]
		public void Message_on_restore_order_without_price()
		{
			restore = true;
			session.DeleteEach<Order>();

			var offer = session.Query<Offer>().First();
			var maxId = session.Query<Price>().ToArray().Max(p => p.Id.PriceId);
			var price = new Price("Тест") {
				RegionName = "Воронеж",
				Id = {
					PriceId = maxId + 10,
					RegionId = offer.Price.Id.RegionId
				},
				RegionId = offer.Price.Id.RegionId
			};
			session.Save(price);
			session.Flush();
			var order = new Order(price, address) {
				Frozen = true
			};
			var orderLine = new OrderLine {
				Order = order,
				Count = 1,
				OfferId = new OfferComposedId()
			};
			orderLine.Clone(offer);
			order.AddLine(orderLine);
			session.Save(order);
			var cmd = InitCmd(new UnfreezeCommand<Order>(order.Id) { Restore = true });
			cmd.Execute();
			var text = (string)cmd.Result;
			var message = String.Format(@"адрес доставки {0}
    прайс-лист {1} - Заказ был заморожен, т.к. прайс-листа нет в обзоре
", address.Name, price.Name);
			Assert.AreEqual(message, text);
		}

		[Test]
		public void Message_on_not_found_price()
		{
			restore = true;
			session.DeleteEach<Order>();

			var offer = session.Query<Offer>().First();
			var maxId = session.Query<Price>().ToArray().Max(p => p.Id.PriceId);
			var price = new Price("Тест") {
				RegionName = "Воронеж",
				Id = {
					PriceId = maxId + 10,
					RegionId = offer.Price.Id.RegionId
				},
				RegionId = offer.Price.Id.RegionId
			};
			session.Save(price);
			session.Flush();
			var order = new Order(price, address) {
				Frozen = true
			};
			var orderLine = new OrderLine {
				Order = order,
				Count = 1,
				OfferId = new OfferComposedId()
			};
			orderLine.Clone(offer);
			order.AddLine(orderLine);
			session.Save(order);
			var cmd = InitCmd(new UnfreezeCommand<Order>(order.Id));
			cmd.Execute();
			var text = (string)cmd.Result;
			var message = String.Format("Заказ №{0} невозможно \"разморозить\", т.к. прайс-листа Тест - Воронеж нет в обзоре\r\n", order.Id);
			Assert.AreEqual(message, text);
		}
	}
}
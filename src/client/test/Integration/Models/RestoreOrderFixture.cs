using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.Fixtures;
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

			Assert.AreEqual(
				$@"адрес доставки {address.Name}
    прайс-лист {offer.Price.Name}
        {offer.ProductSynonym} - {offer.ProducerSynonym}: имеется различие в цене препарата (старая цена: {oldCost:C}; новая цена: {offer.Cost:C})
", cmd.Result);
			var orders = session.Query<Order>().ToArray();
			Assert.AreEqual(1, orders.Length);
			order = orders[0];
			var line = order.Lines[0];
			Assert.AreEqual(LineResultStatus.CostChanged, line.SendResult);
			var message = $"имеется различие в цене препарата (старая цена: {oldCost:C}; новая цена: {offer.Cost:C})";
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
			var message =
				$@"адрес доставки {address.Name}
    прайс-лист {price.Name} - Заказ был заморожен, т.к. прайс-листа нет в обзоре
";
			Assert.AreEqual(message, text);
		}

		[Test]
		public void Message_on_not_found_price()
		{
			restore = true;
			session.DeleteEach<Order>();

			var offer = session.Query<Offer>().First();
			var price = CreatePrice(offer);
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
			var message = $"Заказ №{order.Id} невозможно \"разморозить\", т.к. прайс-листа Тест - Воронеж нет в обзоре\r\n";
			Assert.AreEqual(message, text);
		}

		private Price CreatePrice(Offer offer)
		{
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
			return price;
		}

		[Test]
		public void Ignore_junk_on_reorder()
		{
			restore = true;
			session.DeleteEach<Order>();
			var offer = session.Query<Offer>().First(x => x.RequestRatio == null);
			var offer1 = session.Query<Offer>().First(x => x.ProductId != offer.ProductId);
			var order = MakeOrder(offer);
			var priceDst = CreatePrice(offer);
			var random = new Random();
			var newOffer = new Offer(priceDst, offer, 253);
			newOffer.Id.OfferId += (ulong)random.Next();
			session.Save(newOffer);

			newOffer = new Offer(priceDst, offer, 30);
			newOffer.Junk = true;
			newOffer.Id.OfferId += (ulong)random.Next();
			session.Save(newOffer);

			newOffer = new Offer(priceDst, offer1, 879);
			newOffer.Id.OfferId += (ulong)random.Next();
			session.Save(newOffer);
			MakeOrder(newOffer);

			var cmd = InitCmd(new ReorderCommand<Order>(order.Id));
			cmd.Execute();

			var orders = session.Query<Order>().ToList();
			Assert.AreEqual(1, orders.Count);
			var result = orders[0];
			Assert.AreEqual(priceDst, result.Price);
			Assert.AreEqual(253, result.Lines[1].Cost);
		}
	}
}
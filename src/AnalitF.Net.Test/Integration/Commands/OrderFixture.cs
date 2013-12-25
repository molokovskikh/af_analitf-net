using System;
using System.Linq;
using System.Windows.Forms.VisualStyles;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.Commands
{
	public class OrderFixture : MixedFixture
	{
		[Test]
		public void Load_orders()
		{
			localSession.DeleteEach<Order>();
			var priceId = localSession.Query<Offer>().First().Price.Id.PriceId;
			Fixture(new UnconfirmedOrder(priceId));

			Run(new UpdateCommand());

			var orders = localSession.Query<Order>().ToArray();
			Assert.AreEqual(1, orders.Length);
			var order = orders[0];
			Assert.That(order.Sum, Is.GreaterThan(0));
			Assert.That(order.LinesCount, Is.GreaterThan(0));
			Assert.AreEqual(order.LinesCount, order.Lines.Count);
			Assert.IsFalse(order.Frozen);
			Assert.IsNotNull(order.Address);
			Assert.IsNotNull(order.Price);
		}

		[Test]
		public void Send_orders()
		{
			var order = MakeOrderClean();
			var line = order.Lines[0];

			Run(new SendOrders(address));

			Assert.That(localSession.Query<Order>().Count(), Is.EqualTo(0));
			var sentOrders = localSession.Query<SentOrder>().Where(o => o.SentOn >= begin).ToList();
			Assert.That(sentOrders.Count, Is.EqualTo(1));
			Assert.That(sentOrders[0].Lines.Count, Is.EqualTo(1));

			var orders = session.Query<Common.Models.Order>().Where(o => o.WriteTime >= begin).ToList();
			Assert.That(orders.Count, Is.EqualTo(1));
			var resultOrder = orders[0];
			Assert.That(resultOrder.RowCount, Is.EqualTo(1));
			var item = resultOrder.OrderItems[0];
			Assert.That(item.CodeFirmCr, Is.EqualTo(line.ProducerId));
			Assert.That(item.SynonymCode, Is.EqualTo(line.ProductSynonymId));
			Assert.That(item.SynonymFirmCrCode, Is.EqualTo(line.ProducerSynonymId));

			Assert.That(item.LeaderInfo.MinCost, Is.GreaterThan(0));
			Assert.That(item.LeaderInfo.PriceCode, Is.GreaterThan(0), "номер строки заказа {0}", item.RowId);
		}
	}
}
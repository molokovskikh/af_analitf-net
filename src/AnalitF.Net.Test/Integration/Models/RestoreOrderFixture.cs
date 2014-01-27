using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.NHibernate;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.Models
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
			var restore = new UnfreezeCommand<Order>(order.Id);
			restore.Restore = true;
			restore.Session = session;
			restore.Execute();

			var orders = session.Query<Order>().ToArray();
			Assert.AreEqual(1, orders.Length);
			order = orders[0];
			var line = order.Lines[0];
			Assert.AreEqual(LineResultStatus.CostChanged, line.SendResult);
			var message = String.Format("имеется различие в цене препарата (старая цена: {0:C}; новая цена: {1:C})",
				oldCost, offer.Cost);
			Assert.AreEqual(message, line.LongSendError);
		}
	}
}
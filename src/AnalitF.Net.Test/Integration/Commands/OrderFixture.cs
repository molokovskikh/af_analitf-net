using System.Linq;
using System.Windows.Forms.VisualStyles;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
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
			localSession.Flush();

			var update = new UpdateCommand();
			update.Clean = false;
			update.Config = clientConfig;
			update.Run();

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
	}
}
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Models
{
	[TestFixture]
	public class StatFixture
	{
		[Test]
		public void Load_stat()
		{
			using(var session = SetupFixture.Factory.OpenSession()) {
				session.DeleteEach<Order>();
				var address = session.Query<Address>().First();
				var stat = Stat.Update(session, address);
			}
		}

		[Test]
		public void Ignore_frozen()
		{
			var address = new Address("Тестовый адрес доставки");
			var order = new Order(new Price(), address);
			order.AddLine(new Offer(), 1);
			order.Frozen = true;
			order.Send = false;
			address.Orders.Add(order);
			var stat = new Stat(address);
			Assert.That(stat.OrdersCount, Is.EqualTo(0));
		}
	}
}
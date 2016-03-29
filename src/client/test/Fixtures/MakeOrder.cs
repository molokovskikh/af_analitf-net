using System.Linq;
using AnalitF.Net.Client.Models;
using Common.NHibernate;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class MakeOrder
	{
		public Order Order;

		public void Execute(ISession session)
		{
			session.DeleteEach<Order>();

			var offer = session.Query<Offer>().First(x => x.Junk && x.RequestRatio == null);
			var address = session.Query<Address>().First();

			Order = new Order(address, offer);
			session.Save(Order);
		}
	}
}
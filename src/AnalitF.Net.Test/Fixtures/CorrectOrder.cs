using System.Linq;
using AnalitF.Net.Client.Models;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class CorrectOrder
	{
		public bool Local = true;
		public Order Order;

		public void Execute(ISession session)
		{
			var offer = session.Query<Offer>().First();
			var address = session.Query<Address>().First();

			Order = new Order(offer.Price, address);
			Order.AddLine(offer, 1);
			var offer1 = session.Query<Offer>().First(o => o.Price == Order.Price
				&& o.Id.OfferId != Order.Lines[0].OfferId.OfferId);
			var line = Order.AddLine(offer1, 1);
			line.Apply(new OrderLineResult {
				Result = LineResultStatus.NoOffers
			});

			session.Save(Order);
		}
	}
}
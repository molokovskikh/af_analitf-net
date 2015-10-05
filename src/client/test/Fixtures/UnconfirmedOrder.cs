using System;
using System.Linq;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.Models;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using Test.Support;
using Test.Support.log4net;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class UnconfirmedOrder : ServerFixture
	{
		private uint priceId;
		public TestOrder Order;

		public UnconfirmedOrder()
		{
		}

		public UnconfirmedOrder(uint priceId)
		{
			this.priceId = priceId;
		}

		public override void Execute(ISession session)
		{
			var user = User(session);
			var client = user.Client;
			var user2 = client.CreateUser(session);
			user2.AvaliableAddresses.AddEach(client.Addresses);
			user.AllowDownloadUnconfirmedOrders = true;
			session.CreateSQLQuery("delete from Orders.OrdersHead where Submited = 0 and ClientCode = :clientId")
				.SetParameter("clientId", user.Client.Id)
				.ExecuteUpdate();
			session.CreateSQLQuery("insert into Customers.UserPrices(PriceId, RegionId, UserId) " +
				"select PriceId, RegionId, :target " +
				"from Customers.UserPrices " +
				"where UserId = :source")
				.SetParameter("source", user.Id)
				.SetParameter("target", user2.Id)
				.ExecuteUpdate();
			session.Flush();
			var activePrices = user2.GetActivePricesNaked(session);
			var price = activePrices.Where(p => p.Id.PriceId == priceId)
				.Concat(activePrices)
				.First()
				.Price;
			Order = new TestOrder(user2, price) {
				Submited = false,
				Processed = false
			};
			//уцененные препараты не восстанавливаются
			var offer = session.Query<TestCore>().First(c => c.Price == price && !c.Junk);
			Order.AddItem(offer, 1);
			session.Save(Order);
		}

		public override void Rollback(ISession session)
		{
			User(session).AllowDownloadUnconfirmedOrders = false;
		}
	}
}
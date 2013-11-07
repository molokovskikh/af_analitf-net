using System;
using System.Linq;
using Common.Models;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using Test.Support;
using Test.Support.log4net;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class UnconfirmedOrder
	{
		private uint priceId;

		public bool Local = false;
		public Service.Config.Config Config;

		public UnconfirmedOrder()
		{
		}

		public UnconfirmedOrder(uint priceId)
		{
			this.priceId = priceId;
		}

		public void Execute(ISession session)
		{
			var user = session.Query<TestUser>().First(u => u.Login == Environment.UserName);
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
			var order = new TestOrder(user2, price);
			order.Submited = false;
			order.Processed = false;
			var offer = session.Query<TestCore>().First(c => c.Price == price);
			order.AddItem(offer, 1);
			session.Save(order);
		}

		public void Rollback(ISession session)
		{
			var user = session.Query<TestUser>().First(u => u.Login == Environment.UserName);
			user.AllowDownloadUnconfirmedOrders = false;
		}
	}
}
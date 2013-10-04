﻿using System.Linq;
using AnalitF.Net.Client.Models;
using Common.NHibernate;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class MakeOrder
	{
		public bool Local = true;
		public Order Order;

		public void Execute(ISession session)
		{
			session.DeleteEach<Order>();

			var offer = session.Query<Offer>().First();
			var address = session.Query<Address>().First();

			Order = new Order(offer.Price, address);
			Order.AddLine(offer, 1);
			session.Save(Order);
		}
	}
}
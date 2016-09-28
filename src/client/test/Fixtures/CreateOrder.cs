using System;
using NHibernate;
using Test.Support;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class CreateOrder : ServerFixture
	{
		public override void Execute(ISession session)
		{
			var testUser = User(session);
			var price = testUser.GetActivePrices(session)[0];
			var order = new TestOrder(testUser, price);
			order.WriteTime = DateTime.Now.AddDays(-1);
			order.AddItem(price.Core[0], 1);
			session.Save(order);
		}
	}
}
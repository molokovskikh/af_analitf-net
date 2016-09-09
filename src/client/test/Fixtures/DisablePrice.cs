using System;
using System.Windows;
using NHibernate;
using Test.Support;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class DisablePrice : ServerFixture
	{
		public override void Execute(ISession session)
		{
			var testUser = User(session);
			var price = testUser.GetActivePrices(session)[0];
			price.Enabled = false;
			session.Save(price);
		}
	}
}
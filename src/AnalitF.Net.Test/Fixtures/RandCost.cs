using System;
using System.Linq;
using NHibernate;
using NHibernate.Linq;
using Test.Support;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class RandCost
	{
		public bool Local = false;
		public Service.Config.Config Config;

		public void Execute(ISession session)
		{
			var user = session.Query<TestUser>().First(u => u.Login == Environment.UserName);
			user.UseAdjustmentOrders = true;
			session.CreateSQLQuery("update Farm.CoreCosts set cost = round(rand() * 10000, 2)")
				.ExecuteUpdate();
		}
	}
}
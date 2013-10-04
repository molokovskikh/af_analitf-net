using System;
using System.Linq;
using Devart.Data.MySql;
using NHibernate;
using NHibernate.Exceptions;
using NHibernate.Linq;
using Test.Support;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class RandCost
	{
		public bool Local = false;
		public Service.Config.Config Config;

		public Type Reset = typeof(Reset);

		public void Execute(ISession session)
		{
			var user = session.Query<TestUser>().First(u => u.Login == Environment.UserName);
			user.UseAdjustmentOrders = true;
			try {
				session.CreateSQLQuery("create table Farm.CoreCosts_Backup select * from Farm.CoreCosts")
					.ExecuteUpdate();
			}
			catch(GenericADOException) {
			}

			session.CreateSQLQuery(@"update Farm.CoreCosts set cost = round(rand() * 10000, 2)")
				.ExecuteUpdate();
		}
	}

	public class Reset
	{
		public bool Local = false;
		public Service.Config.Config Config;

		public void Execute(ISession session)
		{
			var user = session.Query<TestUser>().First(u => u.Login == Environment.UserName);
			user.UseAdjustmentOrders = false;
			session.CreateSQLQuery("delete from Farm.CoreCosts;" +
				"insert into Farm.CoreCosts select * from Farm.CoreCosts_Backup;")
				.ExecuteUpdate();
		}
	}
}
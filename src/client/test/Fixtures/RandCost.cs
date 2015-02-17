using System;
using NHibernate;
using NHibernate.Exceptions;
using AnalitF.Net.Service.Models;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class RandCost : ServerFixture
	{
		public override void Execute(ISession session)
		{
			var user = User(session);
			user.UseAdjustmentOrders = true;
			session.CreateSQLQuery(@"update Usersettings.AnalitFReplicationInfo
set ForceReplication = 1
where userId = :userId")
				.SetParameter("userId", user.Id)
				.ExecuteUpdate();
			try {
				session.CreateSQLQuery("create table Farm.CoreCosts_Backup select * from Farm.CoreCosts")
					.ExecuteUpdate();
			}
			catch(GenericADOException) {
			}

			session.CreateSQLQuery(@"update Farm.CoreCosts set cost = round(rand() * 10000, 2)")
				.ExecuteUpdate();
		}

		public override void Rollback(ISession session)
		{
			var user = User(session);
			user.UseAdjustmentOrders = false;
			session.CreateSQLQuery("delete from Farm.CoreCosts;" +
				"insert into Farm.CoreCosts select * from Farm.CoreCosts_Backup;")
				.ExecuteUpdate();
		}
	}
}

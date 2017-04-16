using System.Linq;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using Test.Support;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class CreateDelayOfPayment : ServerFixture
	{
		public override void Execute(ISession session)
		{
			var user = User(session);
			user.ShowSupplierCost = true;
			user.Client.Settings.AllowDelayOfPayment = true;

			var ids = session.Query<TestSupplierIntersection>().Where(i => i.Client == user.Client)
				.SelectMany(i => i.PriceIntersections)
				.Select(i => i.Id).ToArray();
			var delays = session.Query<TestDelayOfPayment>().Where(d => ids.Contains(d.PriceIntersectionId)).ToArray();
			var generator = Generator.Random(500);
			foreach (var delay in delays) {
				delay.OtherDelay = generator.First();
				delay.VitallyImportantDelay = generator.First();
				delay.SupplementDelay = generator.First();
			}
		}

		public override void Rollback(ISession session)
		{
			var user = User(session);
			user.Client.Settings.AllowDelayOfPayment = false;
			session.CreateSQLQuery(@"
update Usersettings.AnalitFReplicationInfo
set ForceReplication = 1
where userId = :userId;")
				.SetParameter("userId", user.Id)
				.ExecuteUpdate();
		}
	}
}
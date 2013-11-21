using System.Linq;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using Test.Support;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class DelayOfPayment : ServerFixture
	{
		public override void Execute(ISession session)
		{
			var user = User(session);
			user.ShowSupplierCost = true;
			user.Client.Settings.AllowAnalitFSchedule = true;

			var ids = session.Query<TestSupplierIntersection>().Where(i => i.Client == user.Client)
				.SelectMany(i => i.PriceIntersections)
				.Select(i => i.Id).ToArray();
			var delays = session.Query<TestDelayOfPayment>().Where(d => ids.Contains(d.PriceIntersectionId)).ToArray();
			var generator = Generator.Random(500);
			foreach (var delay in delays) {
				delay.OtherDelay = generator.First();
				delay.VitallyImportantDelay = generator.First();
			}
		}

		public void Rollback(ISession session)
		{
			var user = User(session);
			user.Client.Settings.AllowAnalitFSchedule = false;
		}
	}
}
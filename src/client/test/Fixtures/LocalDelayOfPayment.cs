using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Test.Integration;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class LocalDelayOfPayment
	{
		public void Execute(ISession session)
		{
			var user = session.Query<User>().First();
			user.IsDeplayOfPaymentEnabled = true;
			user.ShowSupplierCost = true;
			var factors = Generator.RandomDouble();
			foreach (var price in session.Query<Price>()) {
				price.CostFactor = (decimal)factors.First();
				price.VitallyImportantCostFactor = (decimal)factors.First();
			}
		}

		public void Rollback(ISession session)
		{
			IntegrationSetup.RestoreData(session);
		}
	}
}
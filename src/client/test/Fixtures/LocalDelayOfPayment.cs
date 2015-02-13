using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Test.Integration;
using Common.NHibernate;
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
			session.DeleteEach<DelayOfPayment>();
			session.SaveEach(session.Query<Price>().Select(p => new DelayOfPayment((decimal)factors.First(), p) {
				VitallyImportantDelay = (decimal)factors.First()
			}));
		}

		public void Rollback(ISession session)
		{
			DataHelper.RestoreData(session);
		}
	}
}
﻿using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
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
			user.IsDelayOfPaymentEnabled = true;
			user.ShowSupplierCost = true;
			var factors = Generator.RandomDouble();
			session.DeleteEach<DelayOfPayment>();
			session.SaveEach(session.Query<Price>().Select(p => new DelayOfPayment((decimal)factors.First(), p) {
				VitallyImportantDelay = (decimal)factors.First(),
				SupplementDelay = (decimal)factors.First(),
			}));
		}

		public void Rollback(ISession session)
		{
			DbHelper.RestoreData(session);
		}
	}
}
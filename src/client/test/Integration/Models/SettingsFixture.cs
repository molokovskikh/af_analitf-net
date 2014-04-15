using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Models
{
	[TestFixture]
	public class SettingsFixture : DbFixture
	{
		[Test]
		public void Calculate_base_category()
		{
			restore = true;
			var prices = session.Query<Price>().ToList();
			settings.BaseFromCategory = 1;
			var basePrice = prices[0];
			basePrice.Category = 1;
			var notBase = prices.Skip(1).First();
			notBase.Category = 0;
			session.Flush();
			settings.ApplyChanges(session);
			session.Flush();

			session.Refresh(basePrice);
			Assert.IsTrue(basePrice.BasePrice);

			session.Refresh(notBase);
			Assert.IsFalse(notBase.BasePrice);
		}
	}
}
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Models
{
	[TestFixture]
	public class StatFixture
	{
		[Test]
		public void Load_stat()
		{
			using(var session = SetupFixture.Factory.OpenSession()) {
				session.DeleteEach<Order>();
				var address = session.Query<Address>().First();
				var stat = Stat.Update(session, address);
			}
		}
	}
}
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class UnknownWaybill
	{
		public void Execute(ISession session)
		{
			var data = new DataMother(session);
			data.CreateWaybill(session.Query<Address>().First(),
				session.Query<Settings>().First());
		}
	}
}
using AnalitF.Net.Client.Models;
using NHibernate;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class CreateAddress
	{
		public void Execute(ISession session)
		{
			var address = new Address("Тестовый адрес");
			session.Save(address);
			address.Name += " " + address.Id;
			session.Save(address);
		}
	}
}
using System.ComponentModel;
using NHibernate;
using Test.Support;

namespace AnalitF.Net.Client.Test.Fixtures
{
	[Description("Создает адрес доставки на сервере")]
	public class CreateAddress : ServerFixture
	{
		public override void Execute(ISession session)
		{
			var user = User(session);

			var address = user.Client.CreateAddress();
			user.AvaliableAddresses.Add(address);
			session.Save(address);
			address.Value += " " + address.Id;
		}
	}
}
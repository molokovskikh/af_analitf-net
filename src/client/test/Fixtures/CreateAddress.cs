using System.ComponentModel;
using NHibernate;
using Test.Support;

namespace AnalitF.Net.Client.Test.Fixtures
{
	[Description("Создает адрес доставки на сервере")]
	public class CreateAddress : ServerFixture
	{
		public TestAddress Address;

		public override void Execute(ISession session)
		{
			var user = User(session);

			Address = user.Client.CreateAddress();
			user.AvaliableAddresses.Add(Address);
			session.Save(Address);
			Address.Value += " " + Address.Id;
		}

		public override void Rollback(ISession session)
		{
			session.Delete(Address);
		}
	}
}
using NHibernate;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class CreateUser : ServerFixture
	{
		public override void Execute(ISession session)
		{
			var user = User(session);
			var client = user.Client;
			var newUser = client.CreateUser(session);
			foreach (var address in client.Addresses) {
				newUser.JoinAddress(address);
			}
			Log($"Пользователь {user.Id}");
		}
	}
}
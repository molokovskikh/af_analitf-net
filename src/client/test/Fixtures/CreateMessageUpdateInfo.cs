using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate;
using Test.Support;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class CreateMessageUpdateInfo : ServerFixture
	{
		public override void Execute(ISession session)
		{
			var user = User(session);
			session.CreateSQLQuery(@"
update usersettings.userupdateinfo
set MessageShowCount = :MessageShowCount, Message = 'Test Message'
where UserId = :userId;")
				.SetParameter("MessageShowCount", 1)
				.SetParameter("userId", user.Id)
				.ExecuteUpdate();
		}
	}
}
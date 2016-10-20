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
			user.UpdateInfo.Message = "Test Message";
			user.UpdateInfo.MessageShowCount = 1;
			session.Flush();
		}
	}
}
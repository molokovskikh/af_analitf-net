using System;
using System.Linq;
using NHibernate;
using NHibernate.Linq;
using Test.Support;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public abstract class ServerFixture
	{
		public Service.Config.Config Config;
		public bool Verbose;

		public abstract void Execute(ISession session);

		public virtual void Rollback(ISession session)
		{
		}

		public static TestUser User(ISession session)
		{
			return session.Query<TestUser>().First(u => u.Login == Environment.UserName);
		}
	}
}
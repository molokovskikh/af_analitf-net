using System;
using System.Configuration;
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
			var user = session.Query<TestUser>().FirstOrDefault(u => u.Login == DebugLogin());
			if (user == null)
				throw new Exception($"Пользователь {DebugLogin()} для тестирования не найден, выполни rym -f Rymfile.service fixture SampleData для создания тестовых данных");
			return user;
		}

		public static string DebugLogin()
		{
			return ConfigurationManager.AppSettings["DebugUser"] ?? Environment.UserName;
		}

		protected void ExecuteFixture(ServerFixture fixture, ISession session)
		{
			fixture.Config = Config;
			fixture.Verbose = Verbose;
			fixture.Execute(session);
		}
	}
}
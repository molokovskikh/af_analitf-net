using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Web.UI;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Service;
using AnalitF.Net.Service.Config.Environments;
using AnalitF.Net.Service.Test;
using AnalitF.Net.Test.Integration;
using NHibernate;
using NHibernate.Linq;
using Test.Support;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public abstract class ServerFixture
	{
		public Service.Config.Config Config;

		public abstract void Execute(ISession session);

		public virtual void Rollback(ISession session)
		{
		}

		protected static TestUser User(ISession session)
		{
			return session.Query<TestUser>().First(u => u.Login == Environment.UserName);
		}
	}

	public class FixtureHelper : IDisposable
	{
		private List<Action> rollbacks = new List<Action>();

		public T Run<T>()
		{
			return (T)Run(typeof(T));
		}

		public object Run(Type type)
		{
			var fixture = Activator.CreateInstance(type);
			Run(fixture);
			return fixture;
		}

		public void Run(dynamic fixture, bool rollback = false)
		{
			if (!rollback) {
				var reset = Util.GetValue(fixture, "Reset");
				if (reset != null)
					rollbacks.Add(() => Run((Type)reset));
				else if (fixture.GetType().GetMethod("Rollback") != null)
					rollbacks.Add(() => Run(fixture, rollback: true));
			}

			var local = !(fixture is ServerFixture);
			ISessionFactory factory;
			if (local) {
				if (IntegrationSetup.Factory == null) {
					var nhibernate = new Config.Initializers.NHibernate();
					nhibernate.UseRelativePath = true;
					nhibernate.Init();

					factory = nhibernate.Factory;
				}
				else {
					factory = IntegrationSetup.Factory;
				}
			}
			else {
				factory = Setup.SessionFactory;
				fixture.Config = IntegrationSetup.serviceConfig;
				if (factory == null) {
					var config = Application.ReadConfig();
					var development = new Development();
					development.BasePath = Environment.CurrentDirectory;
					development.Run(config);
					fixture.Config = config;
					factory = IntegrationSetup.ServerNHConfig("local");
				}
			}

			using (var session = factory.OpenSession()) {
				using (var transaction = session.BeginTransaction()) {
					if (rollback)
						fixture.Rollback(session);
					else
						fixture.Execute(session);
					transaction.Commit();
				}
			}
		}

		public void Dispose()
		{
			foreach (var rollback in rollbacks) {
				rollback();
			}
		}
	}
}
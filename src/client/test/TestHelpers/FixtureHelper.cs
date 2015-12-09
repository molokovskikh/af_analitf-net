using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Web.UI;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.Integration;
using AnalitF.Net.Service;
using AnalitF.Net.Service.Config.Environments;
using AnalitF.Net.Service.Test;
using NHibernate;
using NHibernate.Hql.Ast.ANTLR.Util;
using NHibernate.Linq;
using Test.Support;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class FixtureHelper : IDisposable
	{
		private List<Action> rollbacks = new List<Action>();
		private bool verbose;

		public FixtureHelper(bool verbose = false)
		{
			this.verbose = verbose;
		}

		public T Run<T>()
		{
			return (T)Run(typeof(T));
		}

		public object Run(Type type)
		{
			try {
				var fixture = Activator.CreateInstance(type);
				Run(fixture);
				return fixture;
			}
			catch(MissingMethodException e) {
				throw new Exception(String.Format("Не удалось создать объект типа {0}", type), e);
			}
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
					factory = GetFactory();
					var config = new Config.Config();
					config.RootDir = Common.Tools.FileHelper.MakeRooted(config.RootDir);
					Util.SetValue(fixture, "Config", IntegrationSetup.clientConfig);
					Util.SetValue(fixture, "Verbose", true);
				}
				else {
					factory = IntegrationSetup.Factory;
					Util.SetValue(fixture, "Config", IntegrationSetup.clientConfig);
				}
			}
			else {
				if (Setup.SessionFactory == null) {
					IntegrationSetup.serviceConfig = Application.ReadConfig();
					var development = new Development();
					development.BasePath = Environment.CurrentDirectory;
					development.Run(IntegrationSetup.serviceConfig);
					Setup.SessionFactory = DbHelper.ServerNHConfig("local");
				}
				fixture.Config = IntegrationSetup.serviceConfig;
				fixture.Verbose = verbose;
				factory = Setup.SessionFactory;
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


		public static ISessionFactory GetFactory()
		{
			if (IntegrationSetup.Factory != null)
				return IntegrationSetup.Factory;
			AppBootstrapper.NHibernate = new Config.NHibernate.NHibernate();
			AppBootstrapper.NHibernate.UseRelativePath = true;
			AppBootstrapper.NHibernate.Init();
			return AppBootstrapper.NHibernate.Factory;
		}

		public void Dispose()
		{
			foreach (var rollback in rollbacks) {
				rollback();
			}
		}
	}
}
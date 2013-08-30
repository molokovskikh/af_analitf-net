using System;
using AnalitF.Net.Service;
using AnalitF.Net.Service.Config.Environments;
using AnalitF.Net.Test.Integration;
using NHibernate;
using Test.Support;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class FixtureHelper
	{
		public static void RunFixture(dynamic fixture)
		{
			var local = fixture.Local;
			ISessionFactory factory;
			if (local) {
				if (SetupFixture.Factory == null) {
					var nhibernate = new Config.Initializers.NHibernate();
					nhibernate.UseRelativePath = true;
					nhibernate.Init();

					factory = nhibernate.Factory;
				}
				else {
					factory = SetupFixture.Factory;
				}
			}
			else {
				if (Setup.SessionFactory == null) {
					var config = Application.ReadConfig();
					var development = new Development();
					development.BasePath = Environment.CurrentDirectory;
					development.Run(config);
					fixture.Config = config;
					Setup.Initialize("local");
				}
				factory = Setup.SessionFactory;
			}

			using (var session = factory.OpenSession()) {
				using (var transaction = session.BeginTransaction()) {
					fixture.Execute(session);
					transaction.Commit();
				}
			}
		}
	}
}
using System;
using AnalitF.Net.Service;
using AnalitF.Net.Service.Config.Environments;
using Test.Support;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class FixtureHelper
	{
		public static void RunFixture(dynamic fixture)
		{
			var local = fixture.Local;
			if (local) {
				var nhibernate = new Config.Initializers.NHibernate();
				nhibernate.UseRelativePath = true;
				nhibernate.Init();

				using (var session = nhibernate.Factory.OpenSession()) {
					using (var transaction = session.BeginTransaction()) {
						fixture.Execute(session);
						transaction.Commit();
					}
				}
			}
			else {
				var config = Application.ReadConfig();
				var development = new Development();
				development.BasePath = Environment.CurrentDirectory;
				development.Run(config);
				fixture.Config = config;
				Setup.Initialize("local");
				using (var session = Setup.SessionFactory.OpenSession()) {
					using (var transaction = session.BeginTransaction()) {
						fixture.Execute(session);
						transaction.Commit();
					}
				}
			}
		}
	}
}
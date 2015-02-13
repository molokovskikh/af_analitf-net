using System.IO;
using System.Linq;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Test.Integration;
using Castle.ActiveRecord;
using Common.Tools;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;
using Test.Support;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class DataHelper
	{
		public static void RestoreData(ISession localSession)
		{
			localSession.CreateSQLQuery("flush tables").ExecuteUpdate();
			Directory.GetFiles(IntegrationSetup.BackupDir)
				.Each(f => File.Copy(f, Path.Combine(IntegrationSetup.clientConfig.DbDir, Path.GetFileName(f)), true));
		}

		public static ISessionFactory ServerNHConfig(string connectionStringName)
		{
			Setup.BuildConfiguration(connectionStringName);
			var server = new Service.Config.Initializers.NHibernate();
			var holder = ActiveRecordMediator.GetSessionFactoryHolder();
			server.Configuration = holder.GetConfiguration(typeof(ActiveRecordBase));
			server.Init();
			return holder.GetSessionFactory(typeof(ActiveRecordBase));
		}

		//создаем пользователя для ручного тестирования
		public static void SeedDb()
		{
			using (var session = Setup.SessionFactory.OpenSession())
			using (session.BeginTransaction()) {
				var user = session.Query<TestUser>().FirstOrDefault(u => u.Login == System.Environment.UserName);
				if (user != null)
					return;
				SampleData.CreateUser(session, System.Environment.UserName);
				session.Transaction.Commit();
			}
		}
	}
}
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.Integration;
using Castle.ActiveRecord;
using Common.Tools;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Environment = System.Environment;

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

		public static void CopyDb(string dir)
		{
			using(var session = IntegrationFixture.Factory.OpenSession()) {
				session.CreateSQLQuery("flush tables").ExecuteUpdate();
			}
			FileHelper.InitDir(dir);
			Directory.GetFiles(IntegrationSetup.clientConfig.DbDir)
				.Each(f => File.Copy(f, Path.Combine(dir, Path.GetFileName(f)), true));
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
				var user = session.Query<TestUser>().FirstOrDefault(u => u.Login == Environment.UserName);
				if (user != null)
					return;
				SampleData.CreateUser(session, Environment.UserName);
				session.Transaction.Commit();
			}
		}

		public static void CopyBin(string src, string dst)
		{
			var regex = new Regex(@"(\.dll|\.exe|\.config|\.pdb)$", RegexOptions.IgnoreCase);
			Directory.GetFiles(src).Where(f => regex.IsMatch(f))
				.Each(f => File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true));
		}

		public static string ProjectBin(string name)
		{
			return Path.Combine(GetRoot(), "src", name, "app", "bin", "debug");
		}

		public static string GetRoot([CallerFilePath] string self = null)
		{
			return GetRootDir(Path.GetDirectoryName(self));
		}

		private static string GetRootDir(string dir)
		{
			if (Directory.Exists(Path.Combine(dir, "src")))
				return dir;
			return GetRootDir(Path.Combine(dir, ".."));
		}

		public static void SaveFailData()
		{
			if (TestContext.CurrentContext.Result.Status == TestStatus.Failed && DispatcherFixture.IsCI()) {
				var root = "fail-test-data";
				var dir = Directory.CreateDirectory(root);
				if (dir.GetDirectories().Length > 10) {
					return;
				}

				CopyDb(Path.Combine(root, FileHelper.StringToPath(TestContext.CurrentContext.Test.FullName)));
			}
		}
	}
}
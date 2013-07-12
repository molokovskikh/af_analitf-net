using System;
using System.IO;
using System.Linq;
using AnalitF.Net.Client;
using Common.Models;
using Common.Tools;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI;
using Test.Support;

namespace AnalitF.Net.Test.Integration
{
	[SetUpFixture]
	public class SetupFixture
	{
		public static ISessionFactory Factory;
		public static Configuration Configuration;

		[SetUp]
		public void Setup()
		{
			AppBootstrapper.InitUi();
			global::Test.Support.Setup.Initialize("server");
			AppBootstrapper.NHibernate = new Client.Config.Initializers.NHibernate();
			AppBootstrapper.NHibernate.Init("client");
			Factory = AppBootstrapper.NHibernate.Factory;
			Configuration = AppBootstrapper.NHibernate.Configuration;

			if (IsStale()) {
				FileHelper.InitDir("data");
				ImportData();
				BackupData();
			}
		}

		private static bool IsStale()
		{
			return !Directory.Exists("data") || !Directory.Exists("backup") || IsDbStale();
		}

		private static bool IsDbStale()
		{
			//если пользователя нет, значит база была перезалита и локальная база не актуальна
			using(var session = IntegrationFixture.Factory.OpenSession()) {
				return !session.Query<TestUser>().Any(u => u.Login == System.Environment.UserName);
			}
		}

		private void ImportData()
		{
			var import = new ExportImportFixture();
			import.IntegrationSetup();
			import.Setup();
			import.Load_data();
			import.Teardown();
			import.IntegrationTearDown();
		}

		private void BackupData()
		{
			using(var session = Factory.OpenSession()) {
				session.CreateSQLQuery("flush tables").ExecuteUpdate();
			}
			FileHelper.InitDir("backup");
			Directory.GetFiles("data")
				.Each(f => File.Copy(f, Path.Combine("backup", Path.GetFileName(f)), true));
		}

		public static void RestoreData(ISession localSession)
		{
			localSession.CreateSQLQuery("flush tables").ExecuteUpdate();
			Directory.GetFiles("backup")
				.Each(f => File.Copy(f, Path.Combine("data", Path.GetFileName(f)), true));
		}
	}
}
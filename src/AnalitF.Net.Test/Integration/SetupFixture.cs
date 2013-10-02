using System;
using System.IO;
using System.Linq;
using AnalitF.Net.Client;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.Tasks;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Castle.ActiveRecord;
using Common.Models;
using Common.Tools;
using Devart.Data.MySql;
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
			Consts.ScrollLoadTimeout = TimeSpan.Zero;
			AppBootstrapper.InitUi();

			//server.Configuration
			global::Test.Support.Setup.BuildConfiguration("server");
			var server = new Service.Config.Initializers.NHibernate();
			var holder = ActiveRecordMediator.GetSessionFactoryHolder();
			server.Configuration = holder.GetConfiguration(typeof(ActiveRecordBase));
			server.Init();
			global::Test.Support.Setup.SessionFactory = holder.GetSessionFactory(typeof(ActiveRecordBase));
			//global::Test.Support.Setup.Initialize("server");

			var nhibernate = new Client.Config.Initializers.NHibernate();
			AppBootstrapper.NHibernate = nhibernate;
			AppBootstrapper.NHibernate.Init("client");
			Factory = nhibernate.Factory;
			Configuration = nhibernate.Configuration;

			if (IsStale()) {
				MySqlConnection.ClearAllPools(true);
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
			using(var localSession = Factory.OpenSession()) {
				try {
					var settings = localSession.Query<Client.Models.Settings>().First();
					if (settings.MappingToken != AppBootstrapper.NHibernate.MappingHash)
						return true;
				}
				catch(Exception) {
					return true;
				}
			}
			//если пользователя нет, значит база была перезалита и локальная база не актуальна
			using(var session = IntegrationFixture.Factory.OpenSession()) {
				return !session.Query<TestUser>().Any(u => u.Login == System.Environment.UserName);
			}
		}

		private void ImportData()
		{
			var sampleData = new SampleData();
			FixtureHelper.RunFixture(sampleData);
			FixtureHelper.RunFixture(new LoadSampleData {
				Files = sampleData.Files,
			});
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
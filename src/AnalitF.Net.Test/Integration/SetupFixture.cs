using System;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;
using AnalitF.Net.Client;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.Tasks;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Service;
using AnalitF.Net.Service.Config;
using Castle.ActiveRecord;
using Common.Models;
using Common.Tools;
using Common.Tools.Calendar;
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
		public static Client.Config.Config clientConfig = new Client.Config.Config();

		public static Config serviceConfig;
		private HttpSelfHostServer server;

		[SetUp]
		public void Setup()
		{
			clientConfig.BaseUrl = new Uri("http://localhost:7018");
			clientConfig.RootDir = "app";
			clientConfig.RequestDelay = 1.Second();

			Consts.ScrollLoadTimeout = TimeSpan.Zero;
			AppBootstrapper.InitUi();

			global::Test.Support.Setup.BuildConfiguration("server");
			var server = new Service.Config.Initializers.NHibernate();
			var holder = ActiveRecordMediator.GetSessionFactoryHolder();
			server.Configuration = holder.GetConfiguration(typeof(ActiveRecordBase));
			server.Init();
			global::Test.Support.Setup.SessionFactory = holder.GetSessionFactory(typeof(ActiveRecordBase));

			var nhibernate = new Client.Config.Initializers.NHibernate();
			AppBootstrapper.NHibernate = nhibernate;
			AppBootstrapper.NHibernate.Init("client");
			Factory = nhibernate.Factory;
			Configuration = nhibernate.Configuration;

			if (IsStale()) {
				MySqlConnection.ClearAllPools(true);

				FileHelper.Persistent(() => FileHelper.InitDir("data"),
					typeof(IOException),
					typeof(UnauthorizedAccessException));
				ImportData();
				BackupData();
			}
			InitWebServer();
		}

		[TearDown]
		public void TearDown()
		{
			server.Dispose();
		}

		public void InitWebServer()
		{
			FileHelper.InitDir("var");
			var cfg = new HttpSelfHostConfiguration(clientConfig.BaseUrl);
			cfg.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
			cfg.ClientCredentialType = HttpClientCredentialType.Windows;

			serviceConfig = Application.InitApp(cfg);
			server = new HttpSelfHostServer(cfg);
			server.OpenAsync();
		}

		private static bool IsStale()
		{
			return !Directory.Exists("data") || !Directory.Exists("backup") || IsDbStale();
		}

		private static bool IsDbStale()
		{
			//если пользователя нет, значит база была перезалита и локальная база не актуальна
			using(var session = IntegrationFixture.Factory.OpenSession()) {
				if (!session.Query<TestUser>().Any(u => u.Login == System.Environment.UserName))
					return true;
			}

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
			return false;
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
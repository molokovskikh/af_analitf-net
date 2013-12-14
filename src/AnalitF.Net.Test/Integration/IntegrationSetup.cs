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
using User = AnalitF.Net.Client.Models.User;

namespace AnalitF.Net.Test.Integration
{
	[SetUpFixture]
	public class IntegrationSetup
	{
		private HttpSelfHostConfiguration cfg;
		private HttpSelfHostServer server;
		private uint serverUserId;

		public static bool isInitialized = false;
		public static ISessionFactory Factory;
		public static Configuration Configuration;
		public static Client.Config.Config clientConfig = new Client.Config.Config();

		public static Config serviceConfig;

		[SetUp]
		public void Setup()
		{
			if (isInitialized) {
				if (server == null) {
					InitWebServer(cfg);
				}
				return;
			}

			clientConfig.BaseUrl = new Uri("http://localhost:7018");
			clientConfig.RootDir = "app";
			clientConfig.RequestInterval = 1.Second();

			FileHelper.InitDir("var");
			Consts.ScrollLoadTimeout = TimeSpan.Zero;
			AppBootstrapper.InitUi();

			global::Test.Support.Setup.SessionFactory = ServerNHConfig("server");

			var nhibernate = new Client.Config.Initializers.NHibernate();
			AppBootstrapper.NHibernate = nhibernate;
			AppBootstrapper.NHibernate.Init("client");
			Factory = nhibernate.Factory;
			Configuration = nhibernate.Configuration;

			cfg = new HttpSelfHostConfiguration(clientConfig.BaseUrl);
			cfg.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
			serviceConfig = Application.InitApp(cfg);

			if (IsServerStale()) {
				FileHelper.InitDir("data", "backup");
			}
			if (IsClientStale()) {
				ImportData();
				BackupData();
			}
			InitWebServer(cfg);
			isInitialized = true;
		}

		[TearDown]
		public void TearDown()
		{
			server.Dispose();
			server = null;
		}

		private bool IsServerStale()
		{
			//если пользователя нет, значит база была перезалита и локальная база не актуальна
			using(var session = IntegrationFixture.Factory.OpenSession()) {
				var user = session.Query<TestUser>().FirstOrDefault(u => u.Login == System.Environment.UserName);
				if (user != null)
					serverUserId = user.Id;
				return user == null;
			}
		}

		private bool IsClientStale()
		{
			new SanityCheck(clientConfig.DbDir).Check();
			using(var session = Factory.OpenSession()) {
				var user = session.Query<User>().FirstOrDefault();
				return user == null || serverUserId != user.Id;
			}
		}

		public static ISessionFactory ServerNHConfig(string connectionStringName)
		{
			global::Test.Support.Setup.BuildConfiguration(connectionStringName);
			var server = new Service.Config.Initializers.NHibernate();
			var holder = ActiveRecordMediator.GetSessionFactoryHolder();
			server.Configuration = holder.GetConfiguration(typeof(ActiveRecordBase));
			server.Init();
			return holder.GetSessionFactory(typeof(ActiveRecordBase));
		}

		public void InitWebServer(HttpSelfHostConfiguration cfg)
		{
			server = new HttpSelfHostServer(cfg);
			server.OpenAsync();
		}

		private void ImportData()
		{
			var sampleData = new SampleData();
			sampleData.Config = serviceConfig;
			var helper = new FixtureHelper();
			helper.Run(sampleData);
			helper.Run(new LoadSampleData {
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
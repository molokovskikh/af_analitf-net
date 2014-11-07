using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq.Observαble;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;
using AnalitF.Net.Client;
using AnalitF.Net.Client.Helpers;
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
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using ReactiveUI;
using Test.Support;
using LogManager = Caliburn.Micro.LogManager;
using User = AnalitF.Net.Client.Models.User;

namespace AnalitF.Net.Test.Integration
{
	[SetUpFixture]
	public class IntegrationSetup
	{
		private HttpSelfHostConfiguration cfg;
		public HttpSelfHostServer server;
		private uint serverUserId;

		public static bool isInitialized = false;
		public static ISessionFactory Factory;
		public static Configuration Configuration;
		public static Client.Config.Config clientConfig = new Client.Config.Config();

		public static Config serviceConfig;
		public static string BackupDir = @"var\client\backup";

		[SetUp]
		public void Setup()
		{
			if (isInitialized) {
				if (server == null) {
					InitWebServer(clientConfig.BaseUrl);
				}
				return;
			}

			if (!Directory.Exists("var"))
				Directory.CreateDirectory("var");

			clientConfig.BaseUrl = new Uri("http://localhost:7018");
			clientConfig.RootDir = @"var\client";
			clientConfig.RequestInterval = 1.Second();
			clientConfig.InitDir();

			Consts.ScrollLoadTimeout = TimeSpan.Zero;
			LogManager.GetLog = t => new Log4net(t);
			AppBootstrapper.InitUi(true);

			global::Test.Support.Setup.SessionFactory = ServerNHConfig("server");
			InitWebServer(clientConfig.BaseUrl);

			var nhibernate = new Client.Config.Initializers.NHibernate();
			AppBootstrapper.NHibernate = nhibernate;
			AppBootstrapper.NHibernate.Init("client");
			Factory = nhibernate.Factory;
			Configuration = nhibernate.Configuration;

			if (IsServerStale()) {
				FileHelper.InitDir(clientConfig.DbDir, BackupDir);
			}
			if (IsClientStale()) {
				ImportData();
				BackupData();
			}
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
			var directoryInfo = new DirectoryInfo(clientConfig.DbDir);
			if (directoryInfo.Exists) {
				var pid = directoryInfo.Parent.GetFiles("*.pid");
				if (pid.Length > 0)
					throw new Exception(String.Format("Существует pid файл {0}, забыл закрыть консоль?", pid[0].FullName));
			}
			var sanityCheck = new SanityCheck();
			sanityCheck.Config = clientConfig;
			//если схема изменилась нужно обновить эталонную копию, иначе в эталонной копии не будет полей
			if (sanityCheck.Check())
				return true;
			using(var session = Factory.OpenSession()) {
				var user = session.Query<User>().FirstOrDefault();
				if (user == null || serverUserId != user.Id)
					return true;
			}
			return false;
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

		public Task InitWebServer(Uri url)
		{
			if (server != null)
				return Task.FromResult(1);
			if (cfg == null) {
				cfg = new HttpSelfHostConfiguration(url);
				cfg.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
				serviceConfig = Application.InitApp(cfg);
			}

			server = new HttpSelfHostServer(cfg);
			return server.OpenAsync();
		}

		private void ImportData()
		{
			var helper = new FixtureHelper();

			var sampleData = helper.Run<SampleData>();
			helper.Run(new LoadSampleData {
				Files = sampleData.Files,
			});
		}

		private void BackupData()
		{
			using(var session = Factory.OpenSession()) {
				session.CreateSQLQuery("flush tables").ExecuteUpdate();
			}
			FileHelper.InitDir(BackupDir);
			Directory.GetFiles(clientConfig.DbDir)
				.Each(f => File.Copy(f, Path.Combine(BackupDir, Path.GetFileName(f)), true));
		}

		public static void RestoreData(ISession localSession)
		{
			localSession.CreateSQLQuery("flush tables").ExecuteUpdate();
			Directory.GetFiles(BackupDir)
				.Each(f => File.Copy(f, Path.Combine(clientConfig.DbDir, Path.GetFileName(f)), true));
		}
	}
}
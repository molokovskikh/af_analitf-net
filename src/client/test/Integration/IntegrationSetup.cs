using System;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Service;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Tools.Threading;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using LogManager = Caliburn.Micro.LogManager;
using User = AnalitF.Net.Client.Models.User;

namespace AnalitF.Net.Client.Test.Integration
{
	[SetUpFixture]
	public class IntegrationSetup
	{
		private HttpSelfHostConfiguration cfg;
		public HttpSelfHostServer server;
		private uint serverUserId;

		public static bool isInitialized;
		public static ISessionFactory Factory;
		public static Configuration Configuration;
		public static Client.Config.Config clientConfig = new Client.Config.Config();

		public static Service.Config.Config serviceConfig;
		public static string BackupDir = @"var\client\backup";

		[OneTimeSetUp]
		public void Setup()
		{
			if (isInitialized) {
				if (server == null) {
					InitWebServer(clientConfig.BaseUrl);
				}
				return;
			}

			Assert.IsNull(server);
			Directory.CreateDirectory("var");

			clientConfig.BaseUrl = InitHelper.RandomPort();
			clientConfig.DiadokApiKey = "Analit-988b9e85-1b8e-40a9-b6bd-543790d0a7ec";
			clientConfig.DiadokUrl = "https://diadoc-api.kontur.ru";
			clientConfig.RootDir = @"var\client";
			clientConfig.RequestInterval = 1.Second();
			clientConfig.InitDir();

			Consts.ScrollLoadTimeout = TimeSpan.Zero;
			LogManager.GetLog = t => new Log4net(t);
			AppBootstrapper.InitUi(true);

			global::Test.Support.Setup.SessionFactory = DbHelper.ServerNHConfig("server");
			InitWebServer(clientConfig.BaseUrl);
			var result = InitHelper.InitService(clientConfig.BaseUrl).Result;
			server = result.Item1;
			serviceConfig = result.Item2;

			var nhibernate = new Client.Config.NHibernate.NHibernate();
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
				DbHelper.CopyDb(BackupDir);
			}
			DbHelper.SeedDb();
			isInitialized = true;
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			server.Dispose();
			server?.Dispose();
			server = null;
		}

		private bool IsServerStale()
		{
			//если пользователя нет, значит база была перезалита и локальная база не актуальна
			using(var session = IntegrationFixture.Factory.OpenSession()) {
				var user = session.Query<TestUser>().FirstOrDefault(u => u.Login == ServerFixture.DebugLogin());
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

		public Task InitWebServer(Uri url)
		{
			if (server != null)
				return Task.FromResult(1);
			if (cfg == null) {
				cfg = new HttpSelfHostConfiguration(url);
				cfg.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
				cfg.HostNameComparisonMode = HostNameComparisonMode.Exact;
				serviceConfig = Application.InitApp(cfg);
				serviceConfig.UpdateLifeTime = TimeSpan.FromDays(1);
			}

			server = new HttpSelfHostServer(cfg);
			return server.OpenAsync();
		}

		private void ImportData()
		{
			var helper = new FixtureHelper();

			using (var cleaner = new FileCleaner()) {
				var sampleData = helper.Run<SampleData>();
				cleaner.Watch(sampleData.Files.Select(x => x.LocalFileName).Where(x => x != null));
				helper.Run(new LoadSampleData(sampleData.Files));
			}
		}

		private void BackupData()
		{
			DbHelper.CopyDb(BackupDir);
		}
	}
}
using System;
using System.IO;
using AnalitF.Net.Client;
using Common.Tools;
using NHibernate;
using NHibernate.Cfg;
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

			if (!Directory.Exists("data")) {
				FileHelper.InitDir("data");
				ImportData();
				BackupData();
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
	}
}
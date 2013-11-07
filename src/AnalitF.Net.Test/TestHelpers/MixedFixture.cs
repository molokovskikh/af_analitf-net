using System;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Service.Test;
using AnalitF.Net.Test.Integration;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class MixedFixture : IntegrationFixture
	{
		private FixtureHelper fixtureHelper;

		protected ISession localSession;
		protected Config.Config clientConfig;
		protected Service.Config.Config serviceConfig;
		protected Settings settings;
		protected Address address;
		protected CompositeDisposable disposable;

		[SetUp]
		public void Setup()
		{
			disposable = new CompositeDisposable();

			fixtureHelper = new FixtureHelper();
			disposable.Add(fixtureHelper);

			clientConfig = SetupFixture.clientConfig;
			serviceConfig = SetupFixture.serviceConfig;

			var files = Directory.GetFiles(".", "*.txt");
			foreach (var file in files) {
				File.Delete(file);
			}

			FileHelper.InitDir(serviceConfig.UpdatePath, clientConfig.RootDir, clientConfig.TmpDir);

			localSession = SetupFixture.Factory.OpenSession();

			settings = localSession.Query<Settings>().First();
			address = localSession.Query<Address>().First();

			ViewModelFixture.StubWindowManager();
		}

		[TearDown]
		public void TearDown()
		{
			if (disposable != null)
				disposable.Dispose();
		}

		protected T Fixture<T>()
		{
			return fixtureHelper.Run<T>();
		}

		protected void Fixture(object fixture)
		{
			fixtureHelper.Run(fixture);
		}
	}
}
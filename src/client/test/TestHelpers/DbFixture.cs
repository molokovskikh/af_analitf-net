using System;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.Integration;
using AnalitF.Net.Client.Test.Tasks;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class DbFixture
	{
		protected CompositeDisposable disposable;

		protected IStatelessSession stateless;
		protected ISession session;
		protected Address address;
		protected User user;
		protected Settings settings;
		protected Config.Config config;
		protected bool restore;

		protected FileCleaner cleaner;

		private FixtureHelper fixtureHelper;

		[SetUp]
		public void DbSetup()
		{
			restore = false;
			disposable = new CompositeDisposable();
			cleaner = new FileCleaner();
			disposable.Add(cleaner);

			fixtureHelper = new FixtureHelper();
			disposable.Add(fixtureHelper);

			session = IntegrationSetup.Factory.OpenSession();
			disposable.Add(session);
			session.Transaction.Begin();
			stateless = IntegrationSetup.Factory.OpenStatelessSession();
			disposable.Add(stateless);

			config = IntegrationSetup.clientConfig;
			user = session.Query<User>().FirstOrDefault();
			address = session.Query<Address>().FirstOrDefault();
			settings = session.Query<Settings>().FirstOrDefault();
		}

		[TearDown]
		public void DbTearDown()
		{
			config.Quiet = false;
			if (session != null) {
				if (session.Transaction.IsActive)
					session.Transaction.Commit();
			}
			if (restore)
				DbHelper.RestoreData(session);
			disposable?.Dispose();
		}

		protected T Fixture<T>()
		{
			return fixtureHelper.Run<T>();
		}

		protected void Fixture(object fixture)
		{
			fixtureHelper.Run(fixture);
		}

		protected T InitCmd<T>(T cmd) where T : BaseCommand
		{
			cmd.Config = config;
			cmd.Token = new CancellationTokenSource().Token;
			cmd.Session = session;
			cmd.StatelessSession = stateless;
			return cmd;
		}

		protected string TempFile(string filename, string content)
		{
			cleaner.Watch(filename);
			File.WriteAllText(filename, content);
			return filename;
		}
	}
}
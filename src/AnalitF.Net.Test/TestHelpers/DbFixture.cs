using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Test.Integration;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class DbFixture
	{
		protected CompositeDisposable disposable;

		protected ISession session;
		protected Address address;
		protected User user;
		protected Settings settings;
		protected Config.Config config;
		protected bool restore;

		[SetUp]
		public void DbSetup()
		{
			restore = false;
			disposable = new CompositeDisposable();

			session = SetupFixture.Factory.OpenSession();
			session.Transaction.Begin();
			disposable.Add(session);

			config = SetupFixture.clientConfig;
			user = session.Query<User>().FirstOrDefault();
			address = session.Query<Address>().FirstOrDefault();
			settings = session.Query<Settings>().FirstOrDefault();
		}

		[TearDown]
		public void DbTearDown()
		{
			config.Quit = false;
			if (session != null) {
				if (session.Transaction.IsActive)
					session.Transaction.Commit();
			}
			if (restore)
				SetupFixture.RestoreData(session);
			disposable.Dispose();
		}

		protected T Fixture<T>()
		{
			return (T)Fixture(typeof(T));
		}

		protected object Fixture(Type type)
		{
			var fixture = Activator.CreateInstance(type);
			FixtureHelper.RunFixture(fixture);

			var reset = Util.GetValue(fixture, "Reset");
			if (reset != null)
				disposable.Add(Disposable.Create(() => Fixture((Type)reset)));

			return fixture;
		}

		protected T InitCmd<T>(T command) where T : BaseCommand
		{
			command.Config = config;
			command.Token = new CancellationTokenSource().Token;
			return command;
		}
	}
}
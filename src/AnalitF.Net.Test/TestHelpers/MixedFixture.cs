using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Service.Test;
using AnalitF.Net.Test.Integration;
using Common.NHibernate;
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

		protected DateTime begin;

		[SetUp]
		public void Setup()
		{
			begin = DateTime.Now;

			disposable = new CompositeDisposable();

			fixtureHelper = new FixtureHelper();
			disposable.Add(fixtureHelper);

			clientConfig = Net.Test.Integration.IntegrationSetup.clientConfig;
			serviceConfig = Net.Test.Integration.IntegrationSetup.serviceConfig;

			FileHelper.InitDir(serviceConfig.UpdatePath,
				clientConfig.TmpDir,
				Path.Combine(ConfigurationManager.AppSettings["ClientDocPath"], "АналитФАРМАЦИЯ"));

			localSession = Net.Test.Integration.IntegrationSetup.Factory.OpenSession();

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

		protected Order MakeOrder(Address address = null, Offer offer = null)
		{
			address = address ?? this.address;
			using (localSession.BeginTransaction()) {
				offer = offer ?? localSession.Query<Offer>().First();
				var order = new Order(offer.Price, address);
				order.AddLine(offer, 1);
				localSession.Save(order);
				return order;
			}
		}

		protected Order MakeOrderClean(Address address = null, Offer offer = null)
		{
			localSession.DeleteEach<Order>();
			return MakeOrder(address, offer);
		}

		protected UpdateResult Run<T>(T command) where T : RemoteCommand
		{
			localSession.Flush();
			session.Flush();

			if (session.Transaction.IsActive)
				session.Transaction.Commit();

			if (localSession.Transaction.IsActive)
				localSession.Transaction.Commit();

			command.Config = clientConfig;
			return command.Run();
		}

		protected TestUser ServerUser()
		{
			return session.Query<TestUser>().First(u => u.Login == Environment.UserName);
		}
	}
}
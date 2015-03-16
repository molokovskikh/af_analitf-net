using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.Fixtures;
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
		private FileCleaner cleaner;

		[SetUp]
		public void MixedFixtureSetup()
		{
			begin = DateTime.Now;

			cleaner = new FileCleaner();
			disposable = new CompositeDisposable();
			disposable.Add(cleaner);

			fixtureHelper = new FixtureHelper();
			disposable.Add(fixtureHelper);

			clientConfig = Net.Test.Integration.IntegrationSetup.clientConfig;
			serviceConfig = Net.Test.Integration.IntegrationSetup.serviceConfig;

			FileHelper.InitDir(serviceConfig.RtmUpdatePath,
				clientConfig.TmpDir,
				Path.Combine(ConfigurationManager.AppSettings["ClientDocPath"], "АналитФАРМАЦИЯ"));

			localSession = Net.Test.Integration.IntegrationSetup.Factory.OpenSession();

			settings = localSession.Query<Settings>().First();
			address = localSession.Query<Address>().First();

			ViewModelFixture.StubWindowManager();
		}

		[TearDown]
		public void MixedFixtureTearDown()
		{
			if (disposable != null)
				disposable.Dispose();
			DataHelper.SaveFailData();
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
				offer = offer ?? SafeOffer();
				var order = new Order(offer.Price, address);
				order.TryOrder(offer, 1);
				localSession.Save(order);
				return order;
			}
		}

		protected Offer SafeOffer()
		{
			return localSession.Query<Offer>().First(o => !o.Price.Name.Contains("минимальный заказ"));
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
			return ServerFixture.User(session);
		}

		protected string TempFile(string filename, string content)
		{
			cleaner.Watch(filename);
			File.WriteAllText(filename, content, Encoding.Default);
			return filename;
		}
	}
}
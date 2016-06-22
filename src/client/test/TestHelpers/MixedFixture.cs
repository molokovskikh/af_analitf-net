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
using Common.NHibernate;
using Common.Tools;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class MixedFixture : IntegrationFixture2
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
		private Random random;

		[SetUp]
		public void MixedFixtureSetup()
		{
			//в качестве начального значения используется время если оно совпадет то и значения будут идентичные
			//для этого тесты должны иметь общий генератор случайных чисел
			random = new Random();
			begin = DateTime.Now;

			cleaner = new FileCleaner();
			disposable = new CompositeDisposable();
			disposable.Add(cleaner);

			fixtureHelper = new FixtureHelper();
			disposable.Add(fixtureHelper);

			clientConfig = Integration.IntegrationSetup.clientConfig;
			serviceConfig = Integration.IntegrationSetup.serviceConfig;

			FileHelper.InitDir(serviceConfig.RtmUpdatePath,
				clientConfig.TmpDir,
				Path.Combine(ConfigurationManager.AppSettings["ClientDocPath"], "АналитФАРМАЦИЯ"));

			localSession = Integration.IntegrationSetup.Factory.OpenSession();

			settings = localSession.Query<Settings>().First();
			address = localSession.Query<Address>().First();

			ViewModelFixture.StubWindowManager();

			var debugTest = Environment.GetEnvironmentVariable("DEBUG_TEST");
			if (debugTest.Match(TestContext.CurrentContext.Test.Name)) {
				var repository = (Hierarchy)LogManager.GetRepository();
				repository.Configured = true;
				var logger = (Logger)repository.GetLogger("AnalitF.Net");
				if (logger.Level == null || logger.Level > Level.Warn)
					logger.Level = Level.Warn;
				var appender = new ConsoleAppender(new PatternLayout(PatternLayout.DefaultConversionPattern));
				appender.ActivateOptions();
				logger.AddAppender(appender);
			}
		}

		[TearDown]
		public void MixedFixtureTearDown()
		{
			var debugTest = Environment.GetEnvironmentVariable("DEBUG_TEST");
			if (debugTest.Match(TestContext.CurrentContext.Test.Name)) {
				var repository = (Hierarchy)LogManager.GetRepository();
				repository.ResetConfiguration();
				XmlConfigurator.Configure();
			}
			disposable?.Dispose();
			DbHelper.SaveFailData();
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
				//что бы сервер не считал завки дублями нужно генерировать разные коды
				order.Lines[0].CodeCr = order.CreatedOn.AddSeconds(random.Next(-15, 15)).ToString();
				localSession.Save(order);
				return order;
			}
		}

		protected Offer SafeOffer()
		{
			return localSession.Query<Offer>().First(o => !o.Price.Name.Contains("минимальный заказ")
				&& o.RequestRatio == null);
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

			command.Configure(settings, clientConfig);
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
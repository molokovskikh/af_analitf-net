using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Test.Integration;
using Caliburn.Micro;
using Microsoft.Reactive.Testing;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI;
using ReactiveUI.Testing;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class BaseFixture
	{
		private IDisposable disposeTestShedule;

		protected Extentions.WindowManager manager;
		protected ISession session;
		protected TestScheduler testScheduler;

		protected ShellViewModel shell;

		protected Address address;
		protected Settings settings;
		protected bool Restore;

		[SetUp]
		public void BaseFixtureSetup()
		{
			Restore = false;

			RxApp.MessageBus = new MessageBus();
			testScheduler = new TestScheduler();
			BaseScreen.TestSchuduler = testScheduler;
			disposeTestShedule = TestUtils.WithScheduler(testScheduler);

			StubWindowManager();

			session = SetupFixture.Factory.OpenSession();
			address = session.Query<Address>().FirstOrDefault();
			settings = session.Query<Settings>().FirstOrDefault();
			shell = new ShellViewModel();
			ScreenExtensions.TryActivate(shell);
		}

		[TearDown]
		public void BaseFixtureTearDown()
		{
			if (Restore) {
				SetupFixture.RestoreData(session);
			}

			disposeTestShedule.Dispose();
			session.Dispose();
		}

		protected void StubWindowManager()
		{
			manager = new Extentions.WindowManager();
			manager.UnderTest = true;
			var @base = IoC.GetInstance;
			IoC.GetInstance = (type, key) => {
				if (type == typeof(IWindowManager))
					return manager;
				return @base(type, key);
			};
		}

		protected T Init<T>() where T : BaseScreen, new()
		{
			return Init(new T());
		}

		protected T Init<T>(T model) where T : BaseScreen
		{
			model.Parent = shell;
			ScreenExtensions.TryActivate(model);
			return model;
		}

		protected List<string> TrackChanges(INotifyPropertyChanged catalogNameViewModel)
		{
			var changes = new List<string>();
			catalogNameViewModel.PropertyChanged += (sender, args) => changes.Add(args.PropertyName);
			return changes;
		}

		public void MakeDifferentCategory(Catalog catalog)
		{
			var offers = session.Query<Offer>().Where(o => o.CatalogId == catalog.Id).ToList();
			var offer = offers[0];
			var offer1 = offers.First(o => o.Price.Id != offer.Price.Id);

			offer.Price.BasePrice = false;
			offer1.Price.BasePrice = true;
			session.Save(offer1.Price);
			session.Save(offer1.Price);
			session.Flush();
		}

		protected Catalog FindMultiOfferCatalog()
		{
			return session.Query<Catalog>()
				.First(c => c.HaveOffers
					&& session.Query<Offer>().Count(o => o.CatalogId == c.Id) >= 2
					&& session.Query<Offer>().Where(o => o.CatalogId == c.Id)
						.Select(o => o.Price)
						.Distinct()
						.Count() > 1);
		}

		protected SentOrder MakeSentOrder(Offer offer = null)
		{
			if (offer == null)
				offer = session.Query<Offer>().First();

			var order = new Order(offer.Price, address);
			order.AddLine(offer, 1);
			var sentOrder = new SentOrder(order);
			session.Save(sentOrder);
			session.Flush();
			return sentOrder;
		}

		protected Order MakeOrder(Offer offer = null, Address toAddress = null)
		{
			if (offer == null)
				offer = session.Query<Offer>().First();

			var order = new Order(offer.Price, toAddress ?? address);
			order.AddLine(offer, 1);
			offer.OrderLine = order.Lines[0];
			session.Save(order);
			session.Flush();
			return order;
		}
	}
}
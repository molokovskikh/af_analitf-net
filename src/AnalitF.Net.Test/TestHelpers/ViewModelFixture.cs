using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Helpers;
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
	public class ViewModelFixture<T> : ViewModelFixture where T : BaseScreen, new()
	{
		private Lazy<T> lazyModel;

		protected T model
		{
			get { return lazyModel.Value; }
		}

		[SetUp]
		public void Setup()
		{
			lazyModel = new Lazy<T>(Init<T>);
		}

		protected void Reset()
		{
			Close(model);
			lazyModel = new Lazy<T>(Init<T>);
		}
	}

	public class ViewModelFixture : DbFixture
	{
		protected Extentions.WindowManager manager;
		protected TestScheduler testScheduler;
		protected Lazy<ShellViewModel> lazyshell;
		protected DataMother data;
		protected MessageBus bus;
		protected Env Env;

		[SetUp]
		public void BaseFixtureSetup()
		{
			Env = new Env();
			ProcessHelper.UnitTesting = true;
			ProcessHelper.ExecutedProcesses.Clear();

			bus = new MessageBus();
			RxApp.MessageBus = bus;
			RxApp.MessageBus.RegisterScheduler<string>(ImmediateScheduler.Instance, "db");

			testScheduler = new TestScheduler();
			BaseScreen.TestSchuduler = testScheduler;
			disposable.Add(TestUtils.WithScheduler(testScheduler));

			lazyshell = new Lazy<ShellViewModel>(() => {
				var value = new ShellViewModel();
				value.UnitTesting = true;
				value.Config = config;
				value.Env = Env;
				disposable.Add(value);
				ScreenExtensions.TryActivate(value);
				return value;
			});
			manager = StubWindowManager(lazyshell);

			data = new DataMother(session);
		}

		protected virtual ShellViewModel shell
		{
			get { return lazyshell.Value; }
		}

		public static Extentions.WindowManager StubWindowManager(Lazy<ShellViewModel> shell = null)
		{
			var manager = new Extentions.WindowManager();
			manager.UnitTesting = true;
			IoC.GetInstance = (type, key) => {
				if (type == typeof(IWindowManager))
					return manager;
				return Activator.CreateInstance(type);
			};

			IoC.GetAllInstances = type => new[] { Activator.CreateInstance(type) };

			IoC.BuildUp = instance => {
				Util.SetValue(instance, "Manager", manager);
				if (shell != null)
					Util.SetValue(instance, "Shell", shell.Value);
			};

			return manager;
		}

		protected T Init<T>() where T : BaseScreen, new()
		{
			return Init(new T());
		}

		protected T Init<T>(T model) where T : BaseScreen
		{
			if (model.IsInitialized)
				return model;

			session.Flush();
			disposable.Add(model);
			model.Parent = shell;
			model.Env = Env;
			Activate(model);
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
			offer = offer ?? session.Query<Offer>().First();
			var order = new Order(offer.Price, address);
			order.AddLine(offer, 1);
			var sentOrder = new SentOrder(order);
			session.Save(sentOrder);
			session.Flush();
			return sentOrder;
		}

		protected Order MakeOrder(Offer offer = null, Address toAddress = null)
		{
			offer = offer ?? session.Query<Offer>().First();
			var order = new Order(offer.Price, toAddress ?? address);
			order.AddLine(offer, 1);
			offer.OrderLine = order.Lines[0];
			session.Save(order);
			session.Flush();
			return order;
		}

		protected void Deactivate(Screen model)
		{
			ScreenExtensions.TryDeactivate(model, false);
		}

		protected void Close(object model)
		{
			ScreenExtensions.TryDeactivate(model, true);
		}

		protected void Activate(object model)
		{
			ScreenExtensions.TryActivate(model);
		}
	}
}
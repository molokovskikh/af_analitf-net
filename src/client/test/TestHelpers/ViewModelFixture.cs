using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Test.Integration.ViewModes;
using Caliburn.Micro;
using Common.Tools;
using Common.Tools.Calendar;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Microsoft.Reactive.Testing;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI;
using ReactiveUI.Testing;
using Test.Support.log4net;
using LogManager = log4net.LogManager;

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
		public void ViewModelFixtureSetup()
		{
			lazyModel = new Lazy<T>(Init<T>);
		}

		protected void ForceInit()
		{
			Assert.IsNotNull(lazyModel.Value);
			testScheduler.Start();
		}

		protected void Reset()
		{
			Close(model);
			lazyModel = new Lazy<T>(Init<T>);
		}

		protected T Next<T>(IEnumerator<IResult> results)
		{
			Assert.IsTrue(results.MoveNext());
			Assert.IsInstanceOf<T>(results.Current);
			return (T)results.Current;
		}
	}

	public class ViewModelFixture : DbFixture
	{
		protected Extentions.WindowManager manager;
		protected TestScheduler testScheduler;
		protected Lazy<ShellViewModel> lazyshell;
		protected MessageBus bus;
		protected Env Env;
		protected IDictionary<string, object> DebugContext;
		private QueryCatcher catcher;

		[SetUp]
		public void BaseFixtureSetup()
		{
			catcher = null;
			DebugContext = new Dictionary<string, object>();
			Env = new Env {
				IsUnitTesting = true
			};
			ProcessHelper.UnitTesting = true;
			ProcessHelper.ExecutedProcesses.Clear();

			bus = new MessageBus();
			RxApp.MessageBus = bus;
			RxApp.MessageBus.RegisterScheduler<string>(ImmediateScheduler.Instance, "db");

			BaseScreen.TestQueryScheduler = new CurrentThreadTaskScheduler();
			testScheduler = new TestScheduler();
			BaseScreen.TestSchuduler = testScheduler;
			disposable.Add(TestUtils.WithScheduler(testScheduler));

			lazyshell = new Lazy<ShellViewModel>(() => {
				var value = new ShellViewModel();
				value.Config = config;
				value.Env = Env;
				disposable.Add(value);
				ScreenExtensions.TryActivate(value);
				return value;
			});
			manager = StubWindowManager(lazyshell);
			var debugTest = Environment.GetEnvironmentVariable("DEBUG_TEST");
			if (debugTest.Match(TestContext.CurrentContext.Test.Name)) {
				catcher = new QueryCatcher();
				catcher.Appender = new MemoryAppender();
				catcher.Start();
			}
		}

		[TearDown]
		public void TearDown()
		{
			if (catcher != null) {
				var events = ((MemoryAppender)catcher.Appender).GetEvents();
				events.Each(e => Console.WriteLine(e.MessageObject));
				catcher = null;
				var repository = (Hierarchy)LogManager.GetRepository();
				repository.ResetConfiguration();
				XmlConfigurator.Configure();
			}

			if (TestContext.CurrentContext.Result.Status == TestStatus.Failed) {
				if (DebugContext.Count > 0)
					Console.WriteLine(DebugContext.Implode(k => String.Format("{0} = {1}", k.Key, k.Value)));
			}
			DataHelper.SaveFailData();
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
				if (instance != null && shell != null
					&& (instance.GetType().GetProperty("Shell") != null || instance.GetType().GetField("Shell") != null))
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

		protected SentOrder MakeSentOrder(params Offer[] offers)
		{
			offers = offers.DefaultIfEmpty(session.Query<Offer>().First()).ToArray();
			var offer = offers.First();
			var order = new Order(offer.Price, address);
			foreach (var offerToOrder in offers) {
				order.TryOrder(offerToOrder, 1);
			}
			var sentOrder = new SentOrder(order);
			session.Save(sentOrder);
			session.Flush();
			sentOrder.Lines.Each(l => l.ServerId = 1000 * l.Id);
			return sentOrder;
		}

		protected Order MakeOrder(Offer offer = null, Address toAddress = null)
		{
			offer = offer ?? session.Query<Offer>().First();
			var order = new Order(offer.Price, toAddress ?? address);
			order.TryOrder(offer, 1);
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

		protected string WaitNotification()
		{
			return shell.Notifications.Timeout(10.Second()).First();
		}
	}
}
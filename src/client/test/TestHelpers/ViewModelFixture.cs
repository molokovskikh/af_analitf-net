using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.Integration;
using AnalitF.Net.Client.Test.Integration.ViewModels;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Tools.Threading;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Microsoft.Reactive.Testing;
using NHibernate.AdoNet.Util;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI;
using ReactiveUI.Testing;
using Test.Support.log4net;
using CurrentThreadScheduler = Common.Tools.Threading.CurrentThreadScheduler;
using LogManager = log4net.LogManager;
using TaskResult = AnalitF.Net.Client.Models.Results.TaskResult;
using WindowManager = AnalitF.Net.Client.Config.Caliburn.WindowManager;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class ViewModelFixture<T> : ViewModelFixture where T : BaseScreen, new()
	{
		private Lazy<T> lazyModel;

		protected T model => lazyModel.Value;

		[SetUp]
		public void ViewModelFixtureSetup()
		{
			lazyModel = new Lazy<T>(Open<T>);
		}

		protected void ForceInit()
		{
			Assert.IsNotNull(lazyModel.Value);
			scheduler.Start();
		}

		protected void Reset()
		{
			Close(model);
			lazyModel = new Lazy<T>(Open<T>);
		}

		protected void TaskResult(IEnumerable<IResult> result)
		{
			var enumerator = result.GetEnumerator();
			var task = Next<TaskResult>(enumerator).Task;
			if (task.Status == TaskStatus.Created)
				task.Start();
			if (!task.Wait(30.Second()))
				throw new Exception("Не удалось дождаться задачи за 30 секунд");
			enumerator.MoveNext();
		}
	}

	[Apartment(ApartmentState.STA)]
	public class ViewModelFixture : DbFixture
	{
		private MemoryAppender appender;
		protected WindowManager manager;
		protected TestScheduler scheduler;
		protected Lazy<ShellViewModel> lazyshell;
		protected MessageBus bus;
		protected Env Env;
		protected IDictionary<string, object> DebugContext;
		//в некоторых случаях зависает
		protected bool autoStartScheduler = true;

		[SetUp]
		public void BaseFixtureSetup()
		{
			appender = null;
			DebugContext = new Dictionary<string, object>();
			ProcessHelper.UnitTesting = true;
			ProcessHelper.ExecutedProcesses.Clear();

			bus = new MessageBus();
			bus.RegisterScheduler<string>(ImmediateScheduler.Instance, "db");

			scheduler = new TestScheduler();
			Env = Env.Current = new Env(null, bus, scheduler, IntegrationSetup.Factory);
			Env.QueryScheduler = new CurrentThreadScheduler();
			Env.TplUiScheduler = new CurrentThreadScheduler();

			lazyshell = new Lazy<ShellViewModel>(() => {
				session.Flush();
				var value = new ShellViewModel(config);
				disposable.Add(value);
				ScreenExtensions.TryActivate(value);
				return value;
			});
			manager = StubWindowManager(lazyshell);
			var debugTest = Environment.GetEnvironmentVariable("DEBUG_TEST");
			if (debugTest.Match(TestContext.CurrentContext.Test.Name)) {
				appender = new MemoryAppender();
				var sql = new QueryCatcher();
				sql.Appender = appender;
				sql.Start();
				var app = new QueryCatcher("AnalitF.Net.Client");
				app.Appender = appender;
				app.Start();
			}
		}

		[TearDown]
		public void BaseFixtureTearDown()
		{
			if (appender != null) {
				var events = appender.GetEvents();
				foreach (var loggingEvent in events) {
					if (loggingEvent.LoggerName == "NHibernate.SQL")
						Console.WriteLine(new BasicFormatter().Format(SqlProcessor.ExtractArguments(loggingEvent.MessageObject.ToString())));
					else
						Console.WriteLine(loggingEvent.MessageObject);
				}
				appender = null;
				var repository = (Hierarchy)LogManager.GetRepository();
				repository.ResetConfiguration();
				XmlConfigurator.Configure();
			}

			if (DbHelper.IsTestFail()) {
				if (DebugContext != null && DebugContext.Count > 0)
					Console.WriteLine(DebugContext.Implode(k => $"{k.Key} = {k.Value}"));
			}
			DbHelper.SaveFailData();
		}

		protected virtual ShellViewModel shell => lazyshell.Value;

		public static WindowManager StubWindowManager(Lazy<ShellViewModel> shell = null)
		{
			var manager = new WindowManager();
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

		protected T Open<T>() where T : BaseScreen, new()
		{
			return Open(new T());
		}

		protected T Open<T>(T model) where T : BaseScreen
		{
			Init(model);
			shell.NavigateRoot(model);
			if (autoStartScheduler) {
				//load async data
				scheduler.Start();
			}
			return model;
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
			offers = offers.DefaultIfEmpty(session.Query<Offer>().First(x => x.RequestRatio == null)).ToArray();
			var offer = offers.First();
			var order = new Order(offer.Price, address);
			foreach (var offerToOrder in offers) {
				order.TryOrder(offerToOrder, offerToOrder.RequestRatio ?? 1);
			}
			var sentOrder = new SentOrder(order);
			session.Save(sentOrder);
			session.Flush();
			sentOrder.Lines.Each(l => l.ServerId = 1000 * l.Id);
			return sentOrder;
		}

		protected void Deactivate(Screen model)
		{
			ScreenExtensions.TryDeactivate(model, false);
		}

		protected void Close(object model)
		{
			ScreenExtensions.TryDeactivate(model, true);
			if (shell.ActiveItem == model)
				shell.ActiveItem = null;
		}

		protected void Activate(object model)
		{
			ScreenExtensions.TryActivate(model);
		}

		protected string WaitNotification()
		{
			return shell.Notifications.Timeout(10.Second()).First();
		}

		protected T Next<T>(IEnumerator<IResult> results)
		{
			Assert.IsTrue(results.MoveNext());
			Assert.IsInstanceOf<T>(results.Current);
			return (T)results.Current;
		}

		protected T Next<T>(IEnumerable<IResult> results)
		{
			return Next<T>(results.GetEnumerator());
		}
	}
}
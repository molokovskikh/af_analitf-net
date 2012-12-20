using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Microsoft.Reactive.Testing;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	public class BaseFixture
	{
		protected Client.Extentions.WindowManager manager;
		protected ShellViewModel shell;
		protected ISession session;
		protected TestScheduler schedule;
		protected Address address;

		private IDisposable disposeTestShedule;

		[SetUp]
		public void BaseFixtureSetup()
		{
			schedule = new TestScheduler();
			BaseScreen.Scheduler = schedule;
			disposeTestShedule = TestUtils.WithScheduler(schedule);

			StubWindowManager();

			session = SetupFixture.Factory.OpenSession();
			address = session.Query<Address>().FirstOrDefault();
			shell = new ShellViewModel();
		}

		[TearDown]
		public void BaseFixtureTearDown()
		{
			disposeTestShedule.Dispose();
			session.Dispose();
		}

		protected void StubWindowManager()
		{
			manager = new Client.Extentions.WindowManager();
			manager.UnderTest = true;
			var @base = IoC.GetInstance;
			IoC.GetInstance = (type, key) => {
				if (type == typeof(IWindowManager))
					return manager;
				return @base(type, key);
			};
		}

		protected T Init<T>(T model) where T : BaseScreen
		{
			model.Parent = shell;
			ScreenExtensions.TryActivate(model);
			return model;
		}
	}
}
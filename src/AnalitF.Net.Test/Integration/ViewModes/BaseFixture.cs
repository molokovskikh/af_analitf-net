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
		private IDisposable disposeTestShedule;

		protected Client.Extentions.WindowManager manager;
		protected ISession session;
		protected TestScheduler testScheduler;

		protected ShellViewModel shell;

		protected Address address;
		protected Settings settings;

		[SetUp]
		public void BaseFixtureSetup()
		{
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
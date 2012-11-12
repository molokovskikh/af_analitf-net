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

		[SetUp]
		public void Setup()
		{
			schedule = new TestScheduler();
			BaseScreen.Scheduler = schedule;
			TestUtils.WithScheduler(schedule);

			manager = new Client.Extentions.WindowManager();
			manager.UnderTest = true;
			IoC.GetInstance = (type, key) => manager;

			session = SetupFixture.Factory.OpenSession();
			address = session.Query<Address>().FirstOrDefault();
			shell = new ShellViewModel();
		}

		[TearDown]
		public void Teardown()
		{
			session.Dispose();
		}

		protected T Init<T>(T model) where T : BaseScreen
		{
			model.Parent = shell;
			return model;
		}
	}
}
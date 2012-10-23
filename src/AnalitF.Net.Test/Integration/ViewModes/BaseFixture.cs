using System;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Microsoft.Reactive.Testing;
using NHibernate;
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

		[SetUp]
		public void Setup()
		{
			schedule = new TestScheduler();
			BaseScreen.Scheduler = schedule;
			TestUtils.WithScheduler(schedule);

			session = SetupFixture.Factory.OpenSession();
			shell = new ShellViewModel();
			manager = new Client.Extentions.WindowManager();
			manager.UnderTest = true;
			IoC.GetInstance = (type, key) => {
				return manager;
			};
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
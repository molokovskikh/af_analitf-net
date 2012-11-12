using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AnalitF.Net.Controllers;
using AnalitF.Net.Models;
using Common.Models;
using NHibernate;
using NUnit.Framework;
using Test.Support;

namespace AnalitF.Net.Service.Test
{
	[TestFixture]
	public class ControllerFixture : IntegrationFixture
	{
		private MainController controller;
		private ISession localSession;
		private User user;

		[SetUp]
		public void Setup()
		{
			var client = TestClient.CreateNaked();
			session.Save(client);
			session.Flush();
			session.Transaction.Commit();

			localSession = FixtureSetup.Factory.OpenSession();
			localSession.BeginTransaction();

			user = localSession.Load<User>(client.Users[0].Id);
			controller = new MainController {
				Session = localSession,
				CurrentUser = user
			};
		}

		[TearDown]
		public void TearDown()
		{
			localSession.Dispose();
		}

		[Test]
		public void Export_data()
		{
			var response = controller.Get();
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
		}

		[Test]
		public void Do_not_load_stale_data()
		{
			var job = new RequestLog(user);
			job.CreatedOn = DateTime.Now.AddMonths(-1);
			localSession.Save(job);

			var response = controller.Get();
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
		}

		[Test]
		public void Reset_error()
		{
			var job = new RequestLog(user) {
				IsCompleted = true,
				IsFaulted = true
			};
			localSession.Save(job);
			var response = controller.Get();
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
		}

		[Test]
		public void Process_request()
		{
			var job = new RequestLog(user);
			localSession.Save(job);
			localSession.Transaction.Commit();

			var task = MainController.StartJob(job.Id, localSession.SessionFactory);
			task.Wait();
			localSession.Clear();
			localSession.Refresh(job);
			Assert.That(job.Error, Is.Null);
			Assert.That(job.IsCompleted, Is.True);
			Assert.That(job.IsFaulted, Is.False);
		}
	}
}

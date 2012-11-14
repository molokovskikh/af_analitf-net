using System;
using System.Net;
using System.Net.Http;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using NHibernate;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Models
{
	[TestFixture]
	public class TasksFixture
	{
		private ISession session;
		private Task task;
		private CancellationTokenSource cancelletion;

		[SetUp]
		public void Setup()
		{
			session = SetupFixture.Factory.OpenSession();
			Tasks.Uri = new Uri("http://localhost:8080/Main/");
			Tasks.ArchiveFile = "archive.zip";
			Tasks.ExtractPath = ".";
			cancelletion = new CancellationTokenSource();
			var token = cancelletion.Token;
			var progress = new BehaviorSubject<Progress>(new Progress());
			task = Tasks.Update(new NetworkCredential(Environment.UserName, ""), token, progress);
		}

		[Test]
		public void Import()
		{
			session.CreateSQLQuery("delete from offers").ExecuteUpdate();

			task.Start();
			task.Wait();
			Assert.That(task.Exception, Is.Null);
			var offers = session.CreateSQLQuery("select * from offers").List();
			Assert.That(offers.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Cancel_task()
		{
			var c = task.ContinueWith(t => {
				Assert.That(task.IsCanceled, Is.True);
			});
			task.Start();
			task.Wait(100);
			cancelletion.Cancel();
			c.Wait();
		}
	}
}
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using Ionic.Zip;
using NHibernate;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Models
{
	[TestFixture]
	public class TasksFixture
	{
		private ISession session;
		private Task<UpdateResult> task;
		private CancellationTokenSource cancelletion;
		private string updatePath;

		[SetUp]
		public void Setup()
		{
			updatePath = @"..\..\..\data\update";
			Tasks.ExtractPath = "temp";
			var files = Directory.GetFiles(".", "*.txt");
			foreach (var file in files) {
				File.Delete(file);
			}

			FileHelper.InitDir(updatePath, Tasks.ExtractPath);

			session = SetupFixture.Factory.OpenSession();
			Tasks.Uri = new Uri("http://localhost:8080/Main/");
			Tasks.ArchiveFile = Path.Combine(Tasks.ExtractPath, "archive.zip");

			cancelletion = new CancellationTokenSource();
			var token = cancelletion.Token;
			var progress = new BehaviorSubject<Progress>(new Progress());
			task = Tasks.Update(null, token, progress);
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
		public void Import_version_update()
		{
			File.WriteAllBytes(Path.Combine(updatePath, "updater.exe"), new byte[0]);
			File.WriteAllText(Path.Combine(updatePath, "version.txt"), "99.99.99.99");

			task.Start();
			task.Wait();
			Assert.That(task.Result, Is.EqualTo(UpdateResult.UpdatePending));
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
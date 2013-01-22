using System;
using System.IO;
using System.Linq;
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
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using log4net.Config;

namespace AnalitF.Net.Test.Integration.Models
{
	[TestFixture]
	public class TasksFixture : IntegrationFixture
	{
		private ISession localSession;
		private Task<UpdateResult> task;
		private CancellationTokenSource cancelletion;
		private string updatePath;
		private BehaviorSubject<Progress> progress;
		private CancellationToken token;

		[SetUp]
		public void Setup()
		{
			updatePath = @"..\..\..\data\update";
			Tasks.ExtractPath = "temp";
			Tasks.RootPath = "app";
			var files = Directory.GetFiles(".", "*.txt");
			foreach (var file in files) {
				File.Delete(file);
			}

			FileHelper.InitDir(updatePath, Tasks.ExtractPath, Tasks.RootPath);

			localSession = SetupFixture.Factory.OpenSession();
			Tasks.Uri = new Uri("http://localhost:8080/Main/");
			Tasks.ArchiveFile = Path.Combine(Tasks.ExtractPath, "archive.zip");

			cancelletion = new CancellationTokenSource();
			token = cancelletion.Token;
			progress = new BehaviorSubject<Progress>(new Progress());

			task = new Task<UpdateResult>(t => Tasks.UpdateTask(null, token, progress), token);
		}

		[TearDown]
		public void FixtureTearDown()
		{
			localSession.CreateSQLQuery("flush tables").ExecuteUpdate();
			Directory.GetFiles("backup")
				.Each(f => File.Copy(f, Path.Combine("data", Path.GetFileName(f)), true));
		}

		[Test]
		public void Import()
		{
			localSession.CreateSQLQuery("delete from offers").ExecuteUpdate();

			task.Start();
			task.Wait();
			Assert.That(task.Exception, Is.Null);
			var offers = localSession.CreateSQLQuery("select * from offers").List();
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
		public void Send_orders()
		{
			task = new Task<UpdateResult>(t => Tasks.SendOrders(null, token, progress), token);

			var begin = DateTime.Now;
			Offer offer;
			using (localSession.BeginTransaction()) {
				localSession.CreateSQLQuery("delete from orders").ExecuteUpdate();
				var address = localSession.Query<Address>().First();
				offer = localSession.Query<Offer>().First();
				var order = new Order(offer.Price, address);
				order.AddLine(offer, 1);
				localSession.Save(order);
			}

			task.Start();
			task.Wait();

			Assert.That(localSession.Query<Order>().Count(), Is.EqualTo(0));
			var sentOrders = localSession.Query<SentOrder>().Where(o => o.SentOn >= begin).ToList();
			Assert.That(sentOrders.Count, Is.EqualTo(1));
			Assert.That(sentOrders[0].Lines.Count, Is.EqualTo(1));

			var orders = session.Query<TestOrder>().Where(o => o.WriteTime >= begin).ToList();
			Assert.That(orders.Count, Is.EqualTo(1));
			var resultOrder = orders[0];
			Assert.That(resultOrder.RowCount, Is.EqualTo(1));
			var item = resultOrder.Items[0];
			Assert.That(item.CodeFirmCr, Is.EqualTo(offer.ProducerId));
			Assert.That(item.SynonymCode, Is.EqualTo(offer.ProductSynonymId));
			Assert.That(item.SynonymFirmCrCode, Is.EqualTo(offer.ProducerSynonymId));
		}

		[Test]
		public void Repair_data_base()
		{
			Directory.GetFiles("data", "mnns.*").Each(File.Delete);
			File.WriteAllBytes(Path.Combine("data", "markupconfigs.frm"), new byte[0]);

			var result = Tasks.CheckAndRepairDb(cancelletion.Token);

			Assert.That(result, Is.False);
			Assert.That(Directory.GetFiles("data", "mnns.*").Length, Is.EqualTo(3));
			Assert.That(new FileInfo(Path.Combine("data", "markupconfigs.frm")).Length, Is.GreaterThan(0));
		}

		[Test]
		public void Clean_db()
		{
			Tasks.CleanDb(token);

			Assert.That(localSession.Query<Offer>().Count(), Is.EqualTo(0));
			Assert.That(localSession.Query<Settings>().Count(), Is.EqualTo(1));
		}

		[Test]
		public void Import_after_update()
		{
			File.Copy(Directory.GetFiles(@"..\..\..\data\result\").Last(), Tasks.ArchiveFile);
			using(var file = new ZipFile(Tasks.ArchiveFile))
				file.ExtractAll(Tasks.ExtractPath);

			localSession.CreateSQLQuery("delete from offers").ExecuteUpdate();
			Tasks.Import(null, token, progress);
			Assert.That(localSession.Query<Offer>().Count(), Is.GreaterThan(0));
		}
	}
}
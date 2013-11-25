using System;
using System.IO;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Service.Test.TestHelpers;
using Common.Tools;
using log4net.Config;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.Commands
{
	[TestFixture]
	public class WaybillsFixture : MixedFixture
	{
		private bool restore;

		[SetUp]
		public void Setup()
		{
			restore = false;
		}

		[TearDown]
		public void TearDown()
		{
			if (restore)
				Integration.IntegrationSetup.RestoreData(localSession);
		}

		[Test]
		public void Calculate_reject()
		{
			var fixture = Fixture<RejectedWaybill>();

			Run(new UpdateCommand());

			var document = localSession.Query<Waybill>().First(w => w.Id == fixture.Doc.Log.Id);
			Assert.IsTrue(document.IsRejectChanged);
			var line = document.Lines.Last();
			Assert.IsFalse(line.IsRejectCanceled);
			Assert.IsTrue(line.IsRejectNew);
			Assert.IsFalse(line.IsReject);
			Assert.IsNotNull(line.RejectId);
		}

		[Test]
		public void Group_waybill()
		{
			restore = true;
			settings.GroupWaybillsBySupplier = true;
			settings.OpenWaybills = true;

			var fixture = Fixture<LoadWaybill>();
			var command = new UpdateCommand();
			Run(command);

			var path = Path.Combine(settings.MapPath("Waybills"), fixture.Waybill.Supplier.Name);
			var files = Directory.GetFiles(path).Select(Path.GetFileName);
			Assert.That(files, Contains.Item("test.txt"));
			var results = command.Results.OfType<OpenResult>().Implode(r => r.Filename);
			Assert.AreEqual(Path.Combine(path, "test.txt"), results);
		}

		[Test]
		public void Load_only_waybills()
		{
			session.CreateSQLQuery(@"delete from Logs.DocumentSendLogs"
				+ " where UserId = :userId;")
				.SetParameter("userId", ServerUser().Id)
				.ExecuteUpdate();
			var command = new UpdateCommand {
				SyncData = "Waybills",
				Clean = false
			};
			Run(command);

			var files = ZipHelper.lsZip(clientConfig.ArchiveFile).Implode();
			Assert.AreEqual("Новых файлов документов нет.", command.SuccessMessage);
			Assert.AreEqual("LoadedDocuments.meta.txt, LoadedDocuments.txt", files);
		}

		[Test]
		public void Load_waybill_without_file()
		{
			session.CreateSQLQuery(@"delete from Logs.DocumentSendLogs"
				+ " where UserId = :userId;")
				.SetParameter("userId", ServerUser().Id)
				.ExecuteUpdate();
			session.Transaction.Commit();
			Fixture(new LoadWaybill(createFile: false));

			var command = new UpdateCommand {
				SyncData = "Waybills"
			};
			Run(command);

			Assert.AreEqual("Получение документов завершено успешно.", command.SuccessMessage);
		}

		[Test]
		public void Import_waybill()
		{
			var fixture = Fixture<LoadWaybill>();
			var sendLog = fixture.SendLog;
			Run(new UpdateCommand());

			var waybills = localSession.Query<Waybill>().ToList();
			Assert.That(waybills.Count(), Is.GreaterThanOrEqualTo(1));
			Assert.That(waybills[0].Sum, Is.GreaterThan(0));
			Assert.That(waybills[0].RetailSum, Is.GreaterThan(0));

			var path = settings.MapPath("Waybills");
			var files = Directory.GetFiles(path).Select(Path.GetFileName);
			Assert.That(files, Contains.Item(Path.GetFileName(fixture.Filename)));
			session.Refresh(sendLog);
			Assert.IsTrue(sendLog.Committed);
			Assert.IsTrue(sendLog.FileDelivered);
			Assert.IsTrue(sendLog.DocumentDelivered);
		}
	}
}
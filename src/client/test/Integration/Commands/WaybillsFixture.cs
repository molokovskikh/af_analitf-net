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
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.Commands
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
				DataHelper.RestoreData(localSession);
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

			var fixture = Fixture<CreateWaybill>();
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
			Fixture(new CreateWaybill(createFile: false));

			var command = new UpdateCommand {
				SyncData = "Waybills"
			};
			Run(command);

			Assert.AreEqual("Получение документов завершено успешно.", command.SuccessMessage);
			//если была загружена хотя бы разобранная накладная то нужно открыть форму накладных а не папку с накладными
			Assert.AreEqual(1, command.Results.Count, command.Results.Implode());
			Assert.IsInstanceOf<ShellResult>(command.Results[0]);
		}

		[Test]
		public void Open_waybill_view_after_import()
		{
			session.CreateSQLQuery(@"delete from Logs.DocumentSendLogs"
				+ " where UserId = :userId;")
				.SetParameter("userId", ServerUser().Id)
				.ExecuteUpdate();
			session.Transaction.Commit();
			Fixture(new CreateWaybill());

			var command = new UpdateCommand {
				SyncData = "Waybills"
			};

			Assert.AreEqual(UpdateResult.OK, Run(command));
			Assert.AreEqual("Получение документов завершено успешно.", command.SuccessMessage);
			//если была загружена хотя бы разобранная накладная то нужно открыть форму накладных а не папку с накладными
			Assert.AreEqual(1, command.Results.Count, command.Results.Implode());
			Assert.IsInstanceOf<ShellResult>(command.Results[0]);
		}

		[Test]
		public void Import_waybill()
		{
			var fixture = Fixture<CreateWaybill>();
			var sendLog = fixture.SendLog;
			Run(new UpdateCommand());

			var waybills = localSession.Query<Waybill>().Where(w => w.DocType == DocType.Waybill).ToList();
			Assert.That(waybills.Count(), Is.GreaterThanOrEqualTo(1));
			var waybill = waybills.First(w => w.Id == fixture.Document.Id);
			Assert.That(waybill.Sum, Is.GreaterThan(0));
			Assert.That(waybill.RetailSum, Is.GreaterThan(0));
			Assert.IsTrue(waybill.IsNew);

			var path = settings.MapPath("Waybills");
			var files = Directory.GetFiles(path).Select(Path.GetFileName);
			Assert.That(files, Contains.Item(Path.GetFileName(fixture.Filename)));
			session.Refresh(sendLog);
			Assert.IsTrue(sendLog.Committed);
			Assert.IsTrue(sendLog.FileDelivered);
			Assert.IsTrue(sendLog.DocumentDelivered);
		}

		[Test]
		public void Mark_waybill_with_reject()
		{
			var reject = localSession.Query<Client.Models.Reject>().First();
			var waybill = new Waybill(address, localSession.Query<Supplier>().First());
			waybill.AddLine(new WaybillLine(waybill) {
				Product = reject.Product,
				ProductId = reject.ProductId,
				SerialNumber = reject.Series,
				SupplierCost = 42.90m,
				SupplierCostWithoutNds = 36.36m,
				Nds = 18,
				ProducerCost = 28.78m,
				Quantity = 10
			});
			localSession.Save(waybill);

			var cmd = new UpdateCommand();
			cmd.Session = localSession;
			cmd.CalculateRejects(settings);

			localSession.Refresh(waybill);
			Assert.IsTrue(waybill.IsRejectChanged, waybill.Id.ToString());
			var line = waybill.Lines[0];
			localSession.Refresh(line);
			Assert.IsTrue(line.IsRejectNew);
			Assert.AreEqual(reject.Id, line.RejectId, line.Id.ToString());
		}
	}
}
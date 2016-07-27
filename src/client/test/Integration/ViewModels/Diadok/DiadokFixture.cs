using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Diadok;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;
using AnalitF.Net.Client.Test.Fixtures;
using Caliburn.Micro;
using System.Windows.Controls;
using System.Reactive.Concurrency;
using Microsoft.Reactive.Testing;
using System.Threading.Tasks;
using System.Threading;
using System;
using Common.Tools.Helpers;
using System.Windows;
using System.Security.Cryptography.X509Certificates;
using BilateralDocumentStatus = Diadoc.Api.Proto.Documents.BilateralDocument.BilateralDocumentStatus;
using BilateralStatus = Diadoc.Api.Com.BilateralDocumentStatus;
using InvoiceStatus = Diadoc.Api.Com.InvoiceStatus;
using RevocationStatus = Diadoc.Api.Proto.Documents.RevocationStatus;
using DocumentType = Diadoc.Api.Com.DocumentType;
using InvoiceDocumentStatus = Diadoc.Api.Proto.Documents.InvoiceDocument.InvoiceStatus;
using AttachmentType = Diadoc.Api.Proto.Events.AttachmentType;
using TaskResult = AnalitF.Net.Client.Models.Results.TaskResult;
using AnalitF.Net.Client.Helpers;
using System.Text;
using AnalitF.Net.Client.Models.Results;
using System.IO;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class DiadokFixture : DispatcherFixture
	{
		CreateDiadokInbox diadokDatas;
		Index ddkIndex;

		[SetUp]
		public new void Setup()
		{
			base.Setup();
			StartWait();

			Env.Settings = session.Query<Settings>().First();

			Env.Settings.DiadokSignerJobTitle = "Signer";
			Env.Settings.DiadokUsername = ddk.ie_login;
			Env.Settings.DiadokPassword = ddk.ie_passwd;
			Env.Settings.DiadokCert = "Тестовая организация №5627996 - UC Test (Qualified)";
			Env.Settings.DebugUseTestSign = true;

			session.Save(Env.Settings);
			session.Flush();

			Click("ShowExtDocs");

			ddkIndex = shell.ActiveItem as Index;

			dispatcher.Invoke(() => (ddkIndex.Scheduler as TestScheduler).Start());
			scheduler.Start();

			Wait();
			dispatcher.Invoke(() => ddkIndex.DeleteAll());
			Wait();

			diadokDatas = Fixture<CreateDiadokInbox>();
		}

		void Wait()
		{
			if(ddkIndex != null)
			{
				dispatcher.Invoke(() => (ddkIndex.Scheduler as TestScheduler).AdvanceByMs(5000));
				scheduler.AdvanceByMs(5000);
				dispatcher.WaitIdle();
				WaitIdle();
			}
		}

		[Test]
		public void TestA_Load_Inbox()
		{
			Thread.Sleep(TimeSpan.FromSeconds(30));
			dispatcher.Invoke(() => ddkIndex.Reload());
			Wait();

			int outboxCount = -123;
			int inboxCount = -456;

			inboxCount = diadokDatas.GetMessages().Item1.Count;
			dispatcher.Invoke(() =>
			{
				outboxCount = ddkIndex.Items.Value.Count;
				Assert.AreEqual(inboxCount, outboxCount);
			});
		}

		[Test]
		public void Test_B_Sign_Inbox()
		{
			Thread.Sleep(TimeSpan.FromSeconds(30));
			dispatcher.Invoke(() => ddkIndex.Reload());
			
			Sign_Inbox();
			RejectSign_InboxTorg12();
			Revocation_Req();

			var messages = diadokDatas.GetMessages();

			var torg12 = messages.Item2.Where(x => x.Entities[0].AttachmentType == AttachmentType.XmlTorg12 && 
			x.Entities[0].DocumentInfo.XmlTorg12Metadata.Status == BilateralStatus.OutboundWithRecipientSignature &&
			x.Entities[0].DocumentInfo.RevocationStatus != RevocationStatus.RevocationStatusNone).First();

			var invoice = messages.Item2.Where(x => x.Entities[0].AttachmentType == AttachmentType.Invoice && 
			x.Entities[0].DocumentInfo.InvoiceMetadata.Status == InvoiceStatus.OutboundFinished &&
			x.Entities[0].DocumentInfo.RevocationStatus != RevocationStatus.RevocationStatusNone).First();

			diadokDatas.Revocation(torg12);
			diadokDatas.Revocation(invoice);

			Thread.Sleep(TimeSpan.FromSeconds(15));
			dispatcher.Invoke(() => ddkIndex.Reload());

		}

		void Sign_Inbox()
		{
			Wait();

			string torg12id = "";
			string invoiceid = "";
			//Подписываем ТОРГ12
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12);
				torg12id = ddkIndex.CurrentItem.Value.Entity.EntityId;
				var signtorg12ok = ddkIndex.Items.Value.First(e => e.Entity.EntityId == torg12id).Entity.DocumentInfo.XmlTorg12Metadata.DocumentStatus == BilateralDocumentStatus.InboundWithRecipientSignature;
				Assert.IsTrue(!signtorg12ok);
			});

			Wait();
			AsyncClick("Sign");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			Wait();
			Click("Save");
			Wait();

			//подписываем Инвойс
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Invoice);
				invoiceid = ddkIndex.CurrentItem.Value.Entity.EntityId;
				var signinvoice = ddkIndex.Items.Value.First(e => e.Entity.EntityId == invoiceid).Entity.DocumentInfo.InvoiceMetadata.Status == InvoiceStatus.InboundFinished;
				Assert.IsTrue(!signinvoice);
			});
			Wait();
			AsyncClick("Sign");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => ddkIndex.Reload());
			Wait();

			dispatcher.Invoke(() => {
				var signtorg12ok = ddkIndex.Items.Value.First(e => e.Entity.EntityId == torg12id).Entity.DocumentInfo.XmlTorg12Metadata.DocumentStatus == BilateralDocumentStatus.InboundWithRecipientSignature;
				Assert.IsTrue(signtorg12ok);
			});

			dispatcher.Invoke(() => {
				var signinvoice = ddkIndex.Items.Value.First(e => e.Entity.EntityId == invoiceid).Entity.DocumentInfo.InvoiceMetadata.Status == InvoiceStatus.InboundFinished;
				Assert.IsTrue(signinvoice);
			});

			Wait();
		}

		void RejectSign_InboxTorg12()
		{
			Wait();

			var torg12id = "";
			var comment = "ОТКАЗ";

			//проверяем документ ожидающий подписи
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12 && f.Entity.DocumentInfo.XmlTorg12Metadata.DocumentStatus == BilateralDocumentStatus.InboundWaitingForRecipientSignature);
				torg12id = ddkIndex.CurrentItem.Value.Entity.EntityId;
			});

			Wait();
			AsyncClick("Reject");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			InputActiveWindow("Comment", comment);
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => {
				var torg12item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == torg12id);
				var commentEntity = torg12item.Message.Entities.First(x => x.ParentEntityId == torg12id && x.AttachmentType == AttachmentType.SignatureRequestRejection);
				var comment1 =  Encoding.UTF8.GetString(commentEntity.Content?.Data ?? new byte[0]);
				Assert.AreEqual(comment, comment1);
				var torg12rejected = torg12item.Entity.DocumentInfo.XmlTorg12Metadata.DocumentStatus == BilateralDocumentStatus.InboundRecipientSignatureRequestRejected;
				Assert.IsTrue(torg12rejected);
			});

			Wait();
		}

		void Revocation_Req()
		{
			// для подписаных и не подписаных
			Wait();
			
			string torg12id = "";
			string invoceid = "";
			string comment = "АННУЛИРОВНИЕ";
			//для подписаных
			//ТОРГ-12
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12 && f.Entity.DocumentInfo.XmlTorg12Metadata.DocumentStatus == BilateralDocumentStatus.InboundWithRecipientSignature);
				torg12id = ddkIndex.CurrentItem.Value.Entity.EntityId;
			});

			Wait();
			AsyncClick("Revoke");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			InputActiveWindow("Comment", comment);
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => {
				var torg12item= ddkIndex.Items.Value.First(e => e.Entity.EntityId == torg12id);
				var commentEntity = torg12item.Message.Entities.First(x => x.ParentEntityId == torg12id && x.AttachmentType == AttachmentType.RevocationRequest);
				var xml = new XMLDocHelper(commentEntity.Content.Data);
				var comment1 = xml.GetValue("Файл/Документ/СвПредАн/ТекстПредАн");
				Assert.AreEqual(comment, comment1);
				var torg12revoc = torg12item.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationIsRequestedByMe;
				Assert.IsTrue(torg12revoc);
			});
			Wait();

			//ИНВОЙС
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Invoice && f.Entity.DocumentInfo.InvoiceMetadata.InvoiceStatus == InvoiceDocumentStatus.InboundFinished);
				invoceid = ddkIndex.CurrentItem.Value.Entity.EntityId;
			});

			Wait();
			AsyncClick("Revoke");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			InputActiveWindow("Comment","АННУЛИРОВНИЕ");
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => {
				var invoiceItem = ddkIndex.Items.Value.First(e => e.Entity.EntityId == invoceid);
				var commentEntity = invoiceItem.Message.Entities.First(x => x.ParentEntityId == invoceid && x.AttachmentType == AttachmentType.RevocationRequest);
				var xml = new XMLDocHelper(commentEntity.Content.Data);
				var comment1 = xml.GetValue("Файл/Документ/СвПредАн/ТекстПредАн");
				Assert.AreEqual(comment, comment1);
				var invoicerevoced = invoiceItem.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationIsRequestedByMe;
				Assert.IsTrue(invoicerevoced);
			});
			Wait();
		}

		void RejectRevocation_Inbox()
		{
			Wait();

			var torg12id = "";
			var invoiceid = "";
			var comment = "ОТКАЗ АННУЛИРОВНИЯ";

			//подписаный ТОРГ-12
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12 && f.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RequestsMyRevocation);
				torg12id = ddkIndex.CurrentItem.Value.Entity.EntityId;
			});

			Wait();
			AsyncClick("Reject");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			InputActiveWindow("Comment", comment);
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => {
				var torg12item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == torg12id);
				var commentEntity = torg12item.Message.Entities.First(x => x.ParentEntityId == torg12id && x.AttachmentType == AttachmentType.RevocationRequest);
				var str = Encoding.Default.GetString(commentEntity.Content.Data);
				str = str.Replace("xmlns=\"http://www.roseu.org/images/stories/roaming/amendment-request-v1.xsd\"","");
				var xml = new XMLDocHelper(str);
				var comment1 = xml.GetValue("Файл/Документ/СвУведУточ/ТекстУведУточ");
				Assert.AreEqual(comment, comment1);
				var torg12RevocationRejected = torg12item.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationRejected;
				Assert.IsTrue(torg12RevocationRejected);
			});

			Wait();

			//подписаный Инвойс
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Invoice && f.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RequestsMyRevocation);
				invoiceid = ddkIndex.CurrentItem.Value.Entity.EntityId;
			});

			Wait();
			AsyncClick("Reject");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			InputActiveWindow("Comment", comment);
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => {
				var invoiceiditem = ddkIndex.Items.Value.First(e => e.Entity.EntityId == invoiceid);
				var commentEntity = invoiceiditem.Message.Entities.First(x => x.ParentEntityId == invoiceid && x.AttachmentType == AttachmentType.RevocationRequest);
				var str = Encoding.Default.GetString(commentEntity.Content.Data);
				str = str.Replace("xmlns=\"http://www.roseu.org/images/stories/roaming/amendment-request-v1.xsd\"","");
				var xml = new XMLDocHelper(str);
				var comment1 = xml.GetValue("Файл/Документ/СвУведУточ/ТекстУведУточ");
				Assert.AreEqual(comment, comment1);
				var torg12rejected = invoiceiditem.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationRejected;
				Assert.IsTrue(torg12rejected);
			});

			Wait();
		}

		void Open_Document()
		{
			Wait();
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12);
				var result = ddkIndex.Open().GetEnumerator();
				var open = Next<OpenResult>(result);
				Assert.IsTrue(File.Exists(open.Filename), open.Filename);
				Assert.That(open.Filename, Does.Contain(@"<?xml version=""1.0"" encoding=""windows-1251""?>"));
			});
			Wait();
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Invoice);
				var result = ddkIndex.Open().GetEnumerator();
				var open = Next<OpenResult>(result);
				Assert.IsTrue(File.Exists(open.Filename), open.Filename);
				Assert.That(open.Filename, Does.Contain(@"<?xml version=""1.0"" encoding=""windows-1251""?>"));
			});
			Wait();
		}

		void Save_Document()
		{
			Wait();
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12);
				var result = ddkIndex.Save().GetEnumerator();
				var savefile = Next<SaveFileResult>(result);
				var text = File.ReadAllText(savefile.Dialog.FileName, Encoding.Default);
				Assert.That(text, Does.Contain(@"<СвТНО НаимПервДок=""Товарная накладная"" ОКУДПервДок=""0330212"" НомФорм=""ТОРГ-12"">"));
			});
			Wait();
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Invoice);
				var result = ddkIndex.Save().GetEnumerator();
				var savefile = Next<SaveFileResult>(result);
				var text = File.ReadAllText(savefile.Dialog.FileName, Encoding.Default);
				Assert.That(text, Does.Contain(@"<СвСчФакт НомерСчФ=""1"" ДатаСчФ=""01.01.2011"" КодОКВ=""643"">"));
			});
			Wait();
		}

		void Delete_Document()
		{
			Wait();
			var itemsCount = 0;
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12);
				itemsCount = ddkIndex.Items.Value.Count();
			});

			Wait();
			AsyncClick("Delete");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Invoice);
			});

			Wait();
			AsyncClick("Delete");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Invoice);
				var itemsCount1 = ddkIndex.Items.Value.Count();
				Assert.AreEqual(itemsCount - 2, itemsCount1);
			});

			Wait();
		}

		void Print_Document()
		{
			Wait();
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12);
				var result = ddkIndex.PrintItem().GetEnumerator();
				var task = Next<TaskResult>(result);
				task.Task.Start();
				task.Task.Wait();
				Assert.True(File.Exists(task.Task.Result));
			});
			Wait();
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Invoice);
				var result = ddkIndex.PrintItem().GetEnumerator();
				var open = Next<OpenResult>(result);
				Assert.IsTrue(File.Exists(open.Filename), open.Filename);
				Assert.That(open.Filename, Does.Contain(@"<?xml version=""1.0"" encoding=""windows-1251""?>"));
			});
			Wait();
		}

		void Agreement_Sug_Document()
		{

		}

		//Подпись
		//Отказ
		//Аннулирование
		//А - подпись
		//А - отказ
		//Открыть
		//Сохранить
		//Удалить
		//Распечатать
		//Согласование
		// На согласование
		// На подпись
		// Согласовать
		// Отказать в согласовании
	}
}
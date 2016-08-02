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
using Diadoc.Api.Com;
using Diadoc.Api.Proto.Events;
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
			Thread.Sleep(TimeSpan.FromSeconds(15)); 
			diadokDatas = Fixture<CreateDiadokInbox>();
		}

		void Wait()
		{
			if(ddkIndex != null)
			{
				dispatcher.Invoke(() => (ddkIndex.Scheduler as TestScheduler).AdvanceByMs(5000));
				scheduler.AdvanceByMs(5000);
				WaitIdle();
				dispatcher.WaitIdle();
			}
		}

		[Test]
		public void Diadoc_documents_test()
		{
			
			Thread.Sleep(TimeSpan.FromSeconds(30));
			dispatcher.Invoke(() => ddkIndex.Reload());

			Sign_Document();
			RejectSign_InboxTorg12();
			Revocation_Req();

			var messages = diadokDatas.GetMessages();

			Thread.Sleep(TimeSpan.FromSeconds(15));
			diadokDatas.OutBoundInvoices(messages.Item2);
			Thread.Sleep(TimeSpan.FromSeconds(30));

			messages = diadokDatas.GetMessages();

			var torg12_1 = messages.Item2.Where(x => x.Entities[0].AttachmentType == AttachmentType.XmlTorg12 &&
			x.Entities[0].DocumentInfo.XmlTorg12Metadata.Status == BilateralStatus.OutboundWithRecipientSignature &&
			x.Entities[0].DocumentInfo.RevocationStatus == RevocationStatus.RevocationStatusNone).First();
			var torg12_2 = messages.Item2.Where(x => x.Entities[0].AttachmentType == AttachmentType.XmlTorg12 &&
			x.Entities[0].DocumentInfo.XmlTorg12Metadata.Status == BilateralStatus.OutboundWithRecipientSignature &&
			x.Entities[0].DocumentInfo.RevocationStatus == RevocationStatus.RevocationStatusNone).Skip(1).First();

			var invoice_1 = messages.Item2.Where(x => x.Entities[0].AttachmentType == AttachmentType.Invoice &&
			x.Entities[0].DocumentInfo.InvoiceMetadata.Status == InvoiceStatus.OutboundFinished &&
			x.Entities[0].DocumentInfo.RevocationStatus == RevocationStatus.RevocationStatusNone).First();
			var invoice_2 = messages.Item2.Where(x => x.Entities[0].AttachmentType == AttachmentType.Invoice &&
			x.Entities[0].DocumentInfo.InvoiceMetadata.Status == InvoiceStatus.OutboundFinished &&
			x.Entities[0].DocumentInfo.RevocationStatus == RevocationStatus.RevocationStatusNone).Skip(1).First();

			diadokDatas.Revocation(torg12_1);
			diadokDatas.Revocation(torg12_2);
			diadokDatas.Revocation(invoice_1);
			diadokDatas.Revocation(invoice_2);

			Thread.Sleep(TimeSpan.FromSeconds(15));
			dispatcher.Invoke(() => ddkIndex.Reload());

			DenialRevocation_Document();
			SignRevocation_Document();
			Open_Document();
			Save_Document();
			Print_Document();
			Agreement_SugApprove_Document();
			Agreement_SugDenial_Document();
			Sign_SugSign_Document();
			Delete_Document();
		}

		void Sign_Document()
		{
			Wait();

			string torg12id = "";
			string invoiceid = "";
			//Подписываем ТОРГ12 3 документа
			for(int i = 0; i < 3; i++)
			{
				dispatcher.Invoke(() => {
					ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.Skip(i).First(f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12 && f.Entity.DocumentInfo.XmlTorg12Metadata.DocumentStatus == BilateralDocumentStatus.InboundWaitingForRecipientSignature);
					torg12id = ddkIndex.CurrentItem.Value.Entity.EntityId;
				});

				Wait();
				AsyncClick("Sign");
				Wait();
				WaitWindow("АналитФАРМАЦИЯ");
				Wait();
				Click("Save");
				Wait();
			}

			for(int i = 0; i < 3; i++)
			{
				//подписываем Инвойс
				dispatcher.Invoke(() => {
					ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.Skip(i).First(f => f.Entity.DocumentInfo.Type == DocumentType.Invoice && f.Entity.DocumentInfo.InvoiceMetadata.Status == InvoiceStatus.InboundNotFinished);
					invoiceid = ddkIndex.CurrentItem.Value.Entity.EntityId;
				});
				Wait();
				AsyncClick("Sign");
				Wait();
				WaitWindow("АналитФАРМАЦИЯ");
				Wait();
				Click("Save");
				Wait();
			}

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
				var xml = new DiadocXMLHelper(commentEntity);
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
				var xml = new DiadocXMLHelper(commentEntity);
				var comment1 = xml.GetValue("Файл/Документ/СвПредАн/ТекстПредАн");
				Assert.AreEqual(comment, comment1);
				var invoicerevoced = invoiceItem.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationIsRequestedByMe;
				Assert.IsTrue(invoicerevoced);
			});

			Wait();
		}

		void DenialRevocation_Document()
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
				var revReqEntity = torg12item.Message.Entities.First(x => x.ParentEntityId == torg12id && x.AttachmentType == AttachmentType.RevocationRequest);
				var commentEntity = torg12item.Message.Entities.Where(x => x.ParentEntityId == revReqEntity.EntityId).OrderBy(x => x.CreationTime).Last();
				var xml = new DiadocXMLHelper(commentEntity);
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
				var revReqEntity = invoiceiditem.Message.Entities.First(x => x.ParentEntityId == invoiceid && x.AttachmentType == AttachmentType.RevocationRequest);
				var commentEntity = invoiceiditem.Message.Entities.Where(x => x.ParentEntityId == revReqEntity.EntityId).OrderBy(x => x.CreationTime).Last();
				var xml = new DiadocXMLHelper(commentEntity);
				var comment1 = xml.GetValue("Файл/Документ/СвУведУточ/ТекстУведУточ");
				Assert.AreEqual(comment, comment1);
				var torg12rejected = invoiceiditem.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationRejected;
				Assert.IsTrue(torg12rejected);
			});

			Wait();
		}

		void SignRevocation_Document()
		{
			Wait();

			var torg12id = "";
			var invoiceid = "";
			
			//подписаный ТОРГ-12
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12 && f.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RequestsMyRevocation);
				torg12id = ddkIndex.CurrentItem.Value.Entity.EntityId;
			});

			Wait();
			AsyncClick("Sign");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => {
				var torg12item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == torg12id);
				var torg12RevocationSigned = torg12item.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationAccepted;
				Assert.IsTrue(torg12RevocationSigned);
			});

			Wait();

			//подписаный Инвойс
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Invoice && f.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RequestsMyRevocation);
				invoiceid = ddkIndex.CurrentItem.Value.Entity.EntityId;
			});

			Wait();
			AsyncClick("Sign");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => {
				var invoiceiditem = ddkIndex.Items.Value.First(e => e.Entity.EntityId == invoiceid);
				var invoiceSigned = invoiceiditem.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationAccepted;
				Assert.IsTrue(invoiceSigned);
			});

			Wait();
		}

		void Open_Document()
		{
			Wait();

			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12);
			});
			Wait();
			dispatcher.Invoke(() => {
				var result = ddkIndex.Open().GetEnumerator();
				var open = Next<OpenResult>(result);
				Assert.IsTrue(File.Exists(open.Filename), open.Filename);
				var text = File.ReadAllText(open.Filename, Encoding.Default);
				Assert.That(text, Does.Contain(@"<СвТНО НаимПервДок="));
			});

			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Invoice);
			});
			Wait();
			dispatcher.Invoke(() => {
				var result = ddkIndex.Open().GetEnumerator();
				var open = Next<OpenResult>(result);
				Assert.IsTrue(File.Exists(open.Filename), open.Filename);
				var text = File.ReadAllText(open.Filename, Encoding.Default);
				Assert.That(text, Does.Contain(@"<СвСчФакт НомерСчФ="));
			});

			Wait();
		}

		void Save_Document()
		{
			Wait();

			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12);
			});
			Wait();
			dispatcher.Invoke(() => {
				var result = ddkIndex.Save().GetEnumerator();
				var savefile = Next<SaveFileResult>(result);
				savefile.Dialog.FilterIndex = 1;
				var file = cleaner.RandomFile();
				savefile.Dialog.FileName = file;
				result.MoveNext();
				var text = File.ReadAllText(savefile.Dialog.FileName, Encoding.Default);
				Assert.That(text, Does.Contain(@"<СвТНО НаимПервДок="));
			});
			Wait();


			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Invoice);
			});
			Wait();
			dispatcher.Invoke(() => {
				var result = ddkIndex.Save().GetEnumerator();
				var savefile = Next<SaveFileResult>(result);
				savefile.Dialog.FilterIndex = 1;
				var file = cleaner.RandomFile();
				savefile.Dialog.FileName = file;
				result.MoveNext();
				var text = File.ReadAllText(savefile.Dialog.FileName, Encoding.Default);
				Assert.That(text, Does.Contain(@"<СвСчФакт НомерСчФ="));
			});

			Wait();
		}

		void Print_Document()
		{
			Wait();

			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12);
			});
			Wait();
			dispatcher.Invoke(() => {
				var result = ddkIndex.PrintItem().GetEnumerator();
				var task = Next<TaskResult>(result);
				task.Task.Wait();
				var taskResult = (task.Task as Task<string>).Result;
				Assert.True(File.Exists(taskResult));
				Assert.True(File.ReadAllBytes(taskResult).Length > 1024);
			});

			Wait();

			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Invoice);
			});
			dispatcher.Invoke(() => {
				var result = ddkIndex.PrintItem().GetEnumerator();
				var task = Next<TaskResult>(result);
				task.Task.Wait();
				var taskResult = (task.Task as Task<string>).Result;
				Assert.True(File.Exists(taskResult));
				Assert.True(File.ReadAllBytes(taskResult).Length > 1024);
			});

			Wait();
		}

		void Agreement_SugApprove_Document()
		{
			Wait();

			string torg12id = "";
			string invoiceid = "";
			var comment = "НА СОГЛАСОВНИЕ";
			var comment2 = "СОГЛАСОВНИЕ ПОДТВЕРЖДЕНИЕ";

			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12 && 
				f.Entity.DocumentInfo.XmlTorg12Metadata.DocumentStatus == BilateralDocumentStatus.InboundWaitingForRecipientSignature &&
				f.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationStatusNone &&
				(f.Entity.DocumentInfo.ResolutionStatus == null ||
				f.Entity.DocumentInfo.ResolutionStatus?.StatusType == ResolutionStatusType.None));
				torg12id = ddkIndex.CurrentItem.Value.Entity.EntityId;
			});

			Wait();
			AsyncClickSplit("Agreement", "На согласование");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			Wait();
			InputActiveWindow("Comment", comment);
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Invoice && 
				f.Entity.DocumentInfo.InvoiceMetadata.Status == InvoiceStatus.InboundNotFinished &&
				(f.Entity.DocumentInfo.ResolutionStatus == null ||
				f.Entity.DocumentInfo.ResolutionStatus?.StatusType == ResolutionStatusType.None));
				invoiceid = ddkIndex.CurrentItem.Value.Entity.EntityId;
			});

			Wait();
			AsyncClickSplit("Agreement", "На согласование");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			Wait();
			InputActiveWindow("Comment", comment);
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => ddkIndex.Reload());
			Wait();

			// проверяем статус и подтвержадем согласование торг12
			dispatcher.Invoke(() => {
				var item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == torg12id);
				var resreqEntt = item.Message.Entities.First(e => e.ResolutionRequestInfo != null);
				Assert.IsTrue(resreqEntt.ResolutionRequestInfo.RequestType == Diadoc.Api.Proto.Events.ResolutionRequestType.ApprovementRequest);
				var commentData =  Encoding.UTF8.GetString(resreqEntt.Content?.Data ?? new byte[0]);
				Assert.AreEqual(comment, commentData);
				ddkIndex.CurrentItem.Value = item;
			});

			Wait();
			AsyncClickSplit("Agreement", "Согласовать");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			Wait();
			InputActiveWindow("Comment", comment2);
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => {
				var item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == torg12id);
				var resreqEntt = item.Message.Entities.First(e => e.ResolutionInfo != null);
				Assert.IsTrue(resreqEntt.ResolutionInfo.ResolutionType == Diadoc.Api.Proto.Events.ResolutionType.Approve);
				var commentData =  Encoding.UTF8.GetString(resreqEntt.Content?.Data ?? new byte[0]);
				Assert.AreEqual(comment2, commentData);
			});

			Wait();

			// проверяем статус и подтвержадем согласование счет-фактуры
			dispatcher.Invoke(() => {
				var item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == invoiceid);
				var resreqEntt = item.Message.Entities.First(e => e.ResolutionRequestInfo != null);
				Assert.IsTrue(resreqEntt.ResolutionRequestInfo.RequestType == Diadoc.Api.Proto.Events.ResolutionRequestType.ApprovementRequest);
				var commentData =  Encoding.UTF8.GetString(resreqEntt.Content?.Data ?? new byte[0]);
				Assert.AreEqual(comment, commentData);
				ddkIndex.CurrentItem.Value = item;
			});

			Wait();
			AsyncClickSplit("Agreement", "Согласовать");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			Wait();
			InputActiveWindow("Comment", comment2);
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => {
				var item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == invoiceid);
				var resreqEntt = item.Message.Entities.First(e => e.ResolutionInfo != null);
				Assert.IsTrue(resreqEntt.ResolutionInfo.ResolutionType == Diadoc.Api.Proto.Events.ResolutionType.Approve);
				var commentData =  Encoding.UTF8.GetString(resreqEntt.Content?.Data ?? new byte[0]);
				Assert.AreEqual(comment2, commentData);
			});

			Wait();
		}

		void Agreement_SugDenial_Document()
		{
			Wait();

			string torg12id = "";
			string invoiceid = "";
			var comment = "НА СОГЛАСОВНИЕ";
			var comment2 = "СОГЛАСОВНИЕ ОТКАЗ";

			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12 && 
				f.Entity.DocumentInfo.XmlTorg12Metadata.DocumentStatus == BilateralDocumentStatus.InboundWaitingForRecipientSignature &&
				f.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationStatusNone &&
				(f.Entity.DocumentInfo.ResolutionStatus == null ||
				f.Entity.DocumentInfo.ResolutionStatus?.StatusType == ResolutionStatusType.None));
				torg12id = ddkIndex.CurrentItem.Value.Entity.EntityId;
			});

			Wait();
			AsyncClickSplit("Agreement", "На согласование");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			Wait();
			InputActiveWindow("Comment", comment);
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Invoice && 
				f.Entity.DocumentInfo.InvoiceMetadata.Status == InvoiceStatus.InboundNotFinished &&
				(f.Entity.DocumentInfo.ResolutionStatus == null ||
				f.Entity.DocumentInfo.ResolutionStatus?.StatusType == ResolutionStatusType.None));
				invoiceid = ddkIndex.CurrentItem.Value.Entity.EntityId;
			});

			Wait();
			AsyncClickSplit("Agreement", "На согласование");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			Wait();
			InputActiveWindow("Comment", comment);
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => ddkIndex.Reload());
			Wait();

			// проверяем статус и подтвержадем согласование торг12
			dispatcher.Invoke(() => {
				var item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == torg12id);
				var resreqEntt = item.Message.Entities.First(e => e.ResolutionRequestInfo != null);
				Assert.IsTrue(resreqEntt.ResolutionRequestInfo.RequestType == Diadoc.Api.Proto.Events.ResolutionRequestType.ApprovementRequest);
				var commentData =  Encoding.UTF8.GetString(resreqEntt.Content?.Data ?? new byte[0]);
				Assert.AreEqual(comment, commentData);
				ddkIndex.CurrentItem.Value = item;
			});

			Wait();
			AsyncClickSplit("Agreement", "Отказать в согласовании");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			Wait();
			InputActiveWindow("Comment", comment2);
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => {
				var item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == torg12id);
				var resreqEntt = item.Message.Entities.First(e => e.ResolutionInfo != null);
				Assert.IsTrue(resreqEntt.ResolutionInfo.ResolutionType == Diadoc.Api.Proto.Events.ResolutionType.Disapprove);
				var commentData =  Encoding.UTF8.GetString(resreqEntt.Content?.Data ?? new byte[0]);
				Assert.AreEqual(comment2, commentData);
			});

			Wait();

			// проверяем статус и подтвержадем согласование счет-фактуры
			dispatcher.Invoke(() => {
				var item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == invoiceid);
				var resreqEntt = item.Message.Entities.First(e => e.ResolutionRequestInfo != null);
				Assert.IsTrue(resreqEntt.ResolutionRequestInfo.RequestType == Diadoc.Api.Proto.Events.ResolutionRequestType.ApprovementRequest);
				var commentData =  Encoding.UTF8.GetString(resreqEntt.Content?.Data ?? new byte[0]);
				Assert.AreEqual(comment, commentData);
				ddkIndex.CurrentItem.Value = item;
			});

			Wait();
			AsyncClickSplit("Agreement", "Отказать в согласовании");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			Wait();
			InputActiveWindow("Comment", comment2);
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => {
				var item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == invoiceid);
				var resreqEntt = item.Message.Entities.First(e => e.ResolutionInfo != null);
				Assert.IsTrue(resreqEntt.ResolutionInfo.ResolutionType == Diadoc.Api.Proto.Events.ResolutionType.Disapprove);
				var commentData =  Encoding.UTF8.GetString(resreqEntt.Content?.Data ?? new byte[0]);
				Assert.AreEqual(comment2, commentData);
			});

			Wait();
		}

		void Sign_SugSign_Document()
		{
			Wait();

			string torg12id = "";
			var comment = "НА ПОДПИСЬ";

			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12 && 
				f.Entity.DocumentInfo.XmlTorg12Metadata.DocumentStatus == BilateralDocumentStatus.InboundWaitingForRecipientSignature &&
				f.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationStatusNone &&
				(f.Entity.DocumentInfo.ResolutionStatus == null ||
				f.Entity.DocumentInfo.ResolutionStatus?.StatusType == ResolutionStatusType.None));
				torg12id = ddkIndex.CurrentItem.Value.Entity.EntityId;
			});

			Wait();
			AsyncClickSplit("Agreement", "На подпись");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			Wait();
			InputActiveWindow("Comment",comment);
			Wait();
			Click("Save");
			Wait();


			// проверяем статус и подтвержадем подпись торг12
			dispatcher.Invoke(() => {
				var item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == torg12id);
				var resreqEntt = item.Message.Entities.First(e => e.ResolutionRequestInfo != null && e.ResolutionRequestInfo.RequestType == Diadoc.Api.Proto.Events.ResolutionRequestType.SignatureRequest);
				Assert.IsTrue(resreqEntt.ResolutionRequestInfo.RequestType == Diadoc.Api.Proto.Events.ResolutionRequestType.SignatureRequest);
				var comment1 =  Encoding.UTF8.GetString(resreqEntt.Content?.Data ?? new byte[0]);
				Assert.AreEqual(comment, comment1);
				ddkIndex.CurrentItem.Value = item;
			});

			Wait();
			AsyncClick("Sign");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => {
				var item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == torg12id);
				Assert.IsTrue(item.Entity.DocumentInfo.XmlTorg12Metadata.Status == BilateralStatus.InboundWithRecipientSignature);
			});

			Wait();

			// счет фактура не может быть передана на подпись
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
				var itemsCount1 = ddkIndex.Items.Value.Count();
				Assert.AreEqual(itemsCount - 2, itemsCount1);
			});

			Wait();
		}
	}
}
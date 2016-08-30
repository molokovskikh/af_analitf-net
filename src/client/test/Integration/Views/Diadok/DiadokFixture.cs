﻿using System;
using System.Text;
using System.IO;
using System.Linq;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;
using Caliburn.Micro;
using System.Windows.Controls;
using Microsoft.Reactive.Testing;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using BilateralDocumentStatus = Diadoc.Api.Proto.Documents.BilateralDocument.BilateralDocumentStatus;
using BilateralStatus = Diadoc.Api.Com.BilateralDocumentStatus;
using InvoiceStatus = Diadoc.Api.Com.InvoiceStatus;
using RevocationStatus = Diadoc.Api.Proto.Documents.RevocationStatus;
using DocumentType = Diadoc.Api.Com.DocumentType;
using InvoiceDocumentStatus = Diadoc.Api.Proto.Documents.InvoiceDocument.InvoiceStatus;
using AttachmentType = Diadoc.Api.Proto.Events.AttachmentType;
using NonformalizedStatus = Diadoc.Api.Proto.Documents.NonformalizedDocument.NonformalizedDocumentStatus;
using Diadoc.Api.Com;
using TaskResult = AnalitF.Net.Client.Models.Results.TaskResult;
using Common.Tools.Calendar;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Diadok;
using AnalitF.Net.Client.Test.Fixtures;

namespace AnalitF.Net.Client.Test.Integration.Views.Diadok
{
	/*
	[TestFixture]
	public class DiadokFixture : DispatcherFixture
	{
		private CreateDiadokInbox diadokDatas;
		private Index ddkIndex;
		private bool testIgnored;

		[SetUp]
		public void SetupTest()
		{
			StartWait();
			testIgnored = false;
			var ddksettings = session.Query<Settings>().First();
			ddksettings.DebugDiadokSignerINN  = CreateDiadokInbox.ddkConfig.reciever_inn;
			ddksettings.DiadokUsername = CreateDiadokInbox.ddkConfig.reciever_login;
			ddksettings.DiadokPassword = CreateDiadokInbox.ddkConfig.reciever_passwd;
			ddksettings.DebugUseTestSign = true;
			ddksettings.DiadokSignerJobTitle = "Специалист";
			session.Save(ddksettings);
			session.Flush();

			Wait();
			Click("ShowExtDocs");
			activeTab = (UserControl)((Screen)shell.ActiveItem).GetView();
			ddkIndex = shell.ActiveItem as Index;

			Wait();
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

		[TearDown]
		void TestTearDown()
		{
			if(testIgnored)
				DeleteAll_Documents();
		}

		[Test]
		public void Diadoc_documents_test()
		{
			// Ждем пока завершится возможно уже начатый тест, проверяем есть ли документы
			var timeOut = 0;
			var dataReady = false;
			dispatcher.Invoke(() => dataReady = ddkIndex.Items.Value.Count == 0);
			Wait();
			while(!dataReady && timeOut < 10) {
				Thread.Sleep(60.Second());
				dispatcher.Invoke(() => ddkIndex.Reload());
				Wait();
				dispatcher.Invoke(() => dataReady = ddkIndex.Items.Value.Count == 0);
				timeOut++;
			}

			if (timeOut >= 10) {
				testIgnored = true;
				Assert.Ignore("Не удалось получить исходные данные, тест выполняется или не были удалены документы");
			}

			diadokDatas = Fixture<CreateDiadokInbox>();
			//парсинг сертификата
			X509Certificate2 cert = new X509Certificate2();
			cert.Import(Convert.FromBase64String(DiadokFixtureData.CertBin));
			var certFields = X509Helper.ParseSubject(cert.Subject);
			var names = certFields["G"].Split(' ');
			var signerFirstName = names[0];
			var signerSureName = certFields["SN"];
			var signerPatronimic = names[1];
			var signerInn = "";
			if(certFields.Keys.Contains("OID.1.2.643.3.131.1.1"))
				signerInn = certFields["OID.1.2.643.3.131.1.1"];
			if(certFields.Keys.Contains("ИНН"))
				signerInn = certFields["ИНН"];
			Assert.IsNotEmpty(signerFirstName);
			Assert.IsNotEmpty(signerSureName);
			Assert.IsNotEmpty(signerPatronimic);
			Assert.IsNotEmpty(signerInn);

			// тесты диадок
			Thread.Sleep(60.Second());
			dispatcher.Invoke(() => ddkIndex.Reload());
			Wait();

			var docsCount = 0;
			dispatcher.Invoke(() => docsCount = ddkIndex.Items.Value.Count);
			if (docsCount != 25) {
				testIgnored = true;
				Assert.Ignore("Не удалось получить исходные данные, тест выполняется или не были удалены документы");
			}

			// тесты функций диадок
			Sign_Document();
			RejectSign_Document();
			Revocation_Req_Document();

			var messages = diadokDatas.GetMessages();

			// выполняем запрос аннулирования
			Thread.Sleep(15.Second());
			diadokDatas.OutBoundInvoices(messages.Item2);
			Thread.Sleep(30.Second());

			messages = diadokDatas.GetMessages();

			var nonform1 = messages.Item2.First(x => x.Entities[0].DocumentInfo.DocumentType == Diadoc.Api.Proto.DocumentType.Nonformalized &&
			x.Entities[0].DocumentInfo.NonformalizedDocumentMetadata.Status == NonformalizedDocumentStatus.OutboundWithRecipientSignature &&
			x.Entities[0].DocumentInfo.RevocationStatus == RevocationStatus.RevocationStatusNone);
			var nonform2 = messages.Item2.Where(x => x.Entities[0].DocumentInfo.DocumentType == Diadoc.Api.Proto.DocumentType.Nonformalized &&
			x.Entities[0].DocumentInfo.NonformalizedDocumentMetadata.Status == NonformalizedDocumentStatus.OutboundWithRecipientSignature &&
			x.Entities[0].DocumentInfo.RevocationStatus == RevocationStatus.RevocationStatusNone).Skip(1).First();

			var torg121 = messages.Item2.First(x => x.Entities[0].AttachmentType == AttachmentType.XmlTorg12 &&
			x.Entities[0].DocumentInfo.XmlTorg12Metadata.Status == BilateralStatus.OutboundWithRecipientSignature &&
			x.Entities[0].DocumentInfo.RevocationStatus == RevocationStatus.RevocationStatusNone);
			var torg122 = messages.Item2.Where(x => x.Entities[0].AttachmentType == AttachmentType.XmlTorg12 &&
			x.Entities[0].DocumentInfo.XmlTorg12Metadata.Status == BilateralStatus.OutboundWithRecipientSignature &&
			x.Entities[0].DocumentInfo.RevocationStatus == RevocationStatus.RevocationStatusNone).Skip(1).First();

			var invoice1 = messages.Item2.First(x => x.Entities[0].AttachmentType == AttachmentType.Invoice &&
			x.Entities[0].DocumentInfo.InvoiceMetadata.Status == InvoiceStatus.OutboundFinished &&
			x.Entities[0].DocumentInfo.RevocationStatus == RevocationStatus.RevocationStatusNone);
			var invoice2 = messages.Item2.Where(x => x.Entities[0].AttachmentType == AttachmentType.Invoice &&
			x.Entities[0].DocumentInfo.InvoiceMetadata.Status == InvoiceStatus.OutboundFinished &&
			x.Entities[0].DocumentInfo.RevocationStatus == RevocationStatus.RevocationStatusNone).Skip(1).First();

			diadokDatas.Revocation(nonform1);
			diadokDatas.Revocation(nonform2);
			diadokDatas.Revocation(torg121);
			diadokDatas.Revocation(torg122);
			diadokDatas.Revocation(invoice1);
			diadokDatas.Revocation(invoice2);

			Thread.Sleep(15.Second());
			dispatcher.Invoke(() => ddkIndex.Reload());

			// продолжаем тесты
			DenialRevocation_Document();
			SignRevocation_Document();
			Open_Document();
			Save_Document();
			Print_Document();
			Agreement_SugApprove_Document();
			Agreement_SugDenial_Document();
			Sign_SugSign_Document();
			Delete_Document();

			Wait();
		}

		void Sign_Document()
		{
			Wait();

			var nonformid = "";
			var torg12Id = "";
			var invoiceid = "";

			//Подписываем Неформализированный 3 документа
			for(var i = 0; i < 3; i++)
			{
				dispatcher.Invoke(() => {
					ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.Skip(i).First(
						f => f.Entity.DocumentInfo.Type == DocumentType.Nonformalized &&
						f.Entity.DocumentInfo.NonformalizedDocumentMetadata.Status == NonformalizedDocumentStatus.InboundWaitingForRecipientSignature);
					nonformid = ddkIndex.CurrentItem.Value.Entity.EntityId;
				});

				Wait();
				AsyncClick("Sign");
				Wait();
				WaitWindow("АналитФАРМАЦИЯ");
				Wait();
				Click("Save");
				Wait();
			}

			//Подписываем ТОРГ12 3 документа
			for(var i = 0; i < 3; i++)
			{
				dispatcher.Invoke(() => {
					ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.Skip(i).First(
						f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12 &&
						f.Entity.DocumentInfo.XmlTorg12Metadata.DocumentStatus == BilateralDocumentStatus.InboundWaitingForRecipientSignature);
					torg12Id = ddkIndex.CurrentItem.Value.Entity.EntityId;
				});

				Wait();
				AsyncClick("Sign");
				Wait();
				WaitWindow("АналитФАРМАЦИЯ");
				Wait();
				Click("Save");
				Wait();
			}

			for(var i = 0; i < 3; i++)
			{
				//подписываем Инвойс
				dispatcher.Invoke(() => {
					ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.Skip(i).First(
						f => f.Entity.DocumentInfo.Type == DocumentType.Invoice &&
						f.Entity.DocumentInfo.InvoiceMetadata.Status == InvoiceStatus.InboundNotFinished);
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
				var signnonformok = ddkIndex.Items.Value.First(e => e.Entity.EntityId == nonformid)
				.Entity.DocumentInfo.NonformalizedDocumentMetadata.Status == NonformalizedDocumentStatus.InboundWithRecipientSignature;
				Assert.IsTrue(signnonformok);
			});

			dispatcher.Invoke(() => {
				var signtorg12Ok = ddkIndex.Items.Value.First(e => e.Entity.EntityId == torg12Id)
				.Entity.DocumentInfo.XmlTorg12Metadata.DocumentStatus == BilateralDocumentStatus.InboundWithRecipientSignature;
				Assert.IsTrue(signtorg12Ok);
			});

			dispatcher.Invoke(() => {
				var signinvoice = ddkIndex.Items.Value.First(e => e.Entity.EntityId == invoiceid)
				.Entity.DocumentInfo.InvoiceMetadata.Status == InvoiceStatus.InboundFinished;
				Assert.IsTrue(signinvoice);
			});

			Wait();
		}

		private void RejectSign_Document()
		{
			Wait();

			var nonformid = "";
			var torg12Id = "";
			var comment = "ОТКАЗ";

			//неформализированный
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(
					f => f.Entity.DocumentInfo.Type == DocumentType.Nonformalized &&
					f.Entity.DocumentInfo.NonformalizedDocumentMetadata.Status == NonformalizedDocumentStatus.InboundWaitingForRecipientSignature);
				nonformid = ddkIndex.CurrentItem.Value.Entity.EntityId;
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
				var nonformitem = ddkIndex.Items.Value.First(e => e.Entity.EntityId == nonformid);
				var commentEntity = nonformitem.Message.Entities.First(
					x => x.ParentEntityId == nonformid &&
					x.AttachmentType == AttachmentType.SignatureRequestRejection);
				var commentData =  Encoding.UTF8.GetString(commentEntity.Content?.Data ?? new byte[0]);
				Assert.AreEqual(comment, commentData);
				var nonformrejected = nonformitem.Entity.DocumentInfo.NonformalizedDocumentMetadata.Status == NonformalizedDocumentStatus.InboundRecipientSignatureRequestRejected;
				Assert.IsTrue(nonformrejected);
			});

			Wait();

			//торг12
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(
					f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12 &&
					f.Entity.DocumentInfo.XmlTorg12Metadata.DocumentStatus == BilateralDocumentStatus.InboundWaitingForRecipientSignature);
				torg12Id = ddkIndex.CurrentItem.Value.Entity.EntityId;
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
				var torg12item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == torg12Id);
				var commentEntity = torg12item.Message.Entities.First(x => x.ParentEntityId == torg12Id &&
				x.AttachmentType == AttachmentType.SignatureRequestRejection);
				var commentData =  Encoding.UTF8.GetString(commentEntity.Content?.Data ?? new byte[0]);
				Assert.AreEqual(comment, commentData);
				var torg12rejected = torg12item.Entity.DocumentInfo.XmlTorg12Metadata.DocumentStatus == BilateralDocumentStatus.InboundRecipientSignatureRequestRejected;
				Assert.IsTrue(torg12rejected);
			});

			Wait();
		}

		void Revocation_Req_Document()
		{
			Wait();

			var nonformid = "";
			var torg12Id = "";
			var invoceid = "";
			string comment = "АННУЛИРОВНИЕ";

			//неформ
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(
					f => f.Entity.DocumentInfo.Type == DocumentType.Nonformalized &&
					f.Entity.DocumentInfo.NonformalizedDocumentMetadata.Status == NonformalizedDocumentStatus.InboundWithRecipientSignature);
				nonformid = ddkIndex.CurrentItem.Value.Entity.EntityId;
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
				var nonfoemitem = ddkIndex.Items.Value.First(e => e.Entity.EntityId == nonformid);
				var commentEntity = nonfoemitem.Message.Entities.First(
					x => x.ParentEntityId == nonformid &&
					x.AttachmentType == AttachmentType.RevocationRequest);
				var xml = new DiadocXmlHelper(commentEntity);
				var commentData = xml.GetValue("Файл/Документ/СвПредАн/ТекстПредАн");
				Assert.AreEqual(comment, commentData);
				var nonfrevoc = nonfoemitem.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationIsRequestedByMe;
				Assert.IsTrue(nonfrevoc);
			});
			Wait();
			//ТОРГ-12
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12 &&
				f.Entity.DocumentInfo.XmlTorg12Metadata.DocumentStatus == BilateralDocumentStatus.InboundWithRecipientSignature);
				torg12Id = ddkIndex.CurrentItem.Value.Entity.EntityId;
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
				var torg12Item= ddkIndex.Items.Value.First(e => e.Entity.EntityId == torg12Id);
				var commentEntity = torg12Item.Message.Entities.First(x => x.ParentEntityId == torg12Id &&
				x.AttachmentType == AttachmentType.RevocationRequest);
				var xml = new DiadocXmlHelper(commentEntity);
				var commentData = xml.GetValue("Файл/Документ/СвПредАн/ТекстПредАн");
				Assert.AreEqual(comment, commentData);
				var torg12Revoc = torg12Item.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationIsRequestedByMe;
				Assert.IsTrue(torg12Revoc);
			});
			Wait();

			//ИНВОЙС
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Invoice &&
				f.Entity.DocumentInfo.InvoiceMetadata.InvoiceStatus == InvoiceDocumentStatus.InboundFinished);
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
				var commentEntity = invoiceItem.Message.Entities.First(x => x.ParentEntityId == invoceid &&
				x.AttachmentType == AttachmentType.RevocationRequest);
				var xml = new DiadocXmlHelper(commentEntity);
				var commentData = xml.GetValue("Файл/Документ/СвПредАн/ТекстПредАн");
				Assert.AreEqual(comment, commentData);
				var invoicerevoced = invoiceItem.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationIsRequestedByMe;
				Assert.IsTrue(invoicerevoced);
			});

			Wait();
		}

		void DenialRevocation_Document()
		{
			Wait();

			var nonformid = "";
			var torg12Id = "";
			var invoiceid = "";
			var comment = "ОТКАЗ АННУЛИРОВНИЯ";

			//неформализированный
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f =>
				f.Entity.DocumentInfo.DocumentType == Diadoc.Api.Proto.DocumentType.Nonformalized &&
				f.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RequestsMyRevocation);
				nonformid = ddkIndex.CurrentItem.Value.Entity.EntityId;
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
				var nonfitem = ddkIndex.Items.Value.First(e => e.Entity.EntityId == nonformid);
				var revReqEntity = nonfitem.Message.Entities.First(x => x.ParentEntityId == nonformid &&
				x.AttachmentType == AttachmentType.RevocationRequest);
				var commentEntity = nonfitem.Message.Entities.Where(x => x.ParentEntityId == revReqEntity.EntityId)
				.OrderBy(x => x.CreationTime).Last();
				var xml = new DiadocXmlHelper(commentEntity);
				var commentData = xml.GetValue("Файл/Документ/СвУведУточ/ТекстУведУточ");
				Assert.AreEqual(comment, commentData);
				var nonformRevocationRejected = nonfitem.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationRejected;
				Assert.IsTrue(nonformRevocationRejected);
			});

			Wait();

			//ТОРГ-12
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f =>
				f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12 &&
				f.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RequestsMyRevocation);
				torg12Id = ddkIndex.CurrentItem.Value.Entity.EntityId;
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
				var torg12Item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == torg12Id);
				var revReqEntity = torg12Item.Message.Entities.First(x => x.ParentEntityId == torg12Id &&
				x.AttachmentType == AttachmentType.RevocationRequest);
				var commentEntity = torg12Item.Message.Entities.Where(x => x.ParentEntityId == revReqEntity.EntityId)
				.OrderBy(x => x.CreationTime).Last();
				var xml = new DiadocXmlHelper(commentEntity);
				var commentData = xml.GetValue("Файл/Документ/СвУведУточ/ТекстУведУточ");
				Assert.AreEqual(comment, commentData);
				var torg12RevocationRejected = torg12Item.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationRejected;
				Assert.IsTrue(torg12RevocationRejected);
			});

			Wait();

			//Инвойс
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Invoice &&
				f.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RequestsMyRevocation);
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
				var revReqEntity = invoiceiditem.Message.Entities.First(x => x.ParentEntityId == invoiceid &&
				x.AttachmentType == AttachmentType.RevocationRequest);
				var commentEntity = invoiceiditem.Message.Entities.Where(x => x.ParentEntityId == revReqEntity.EntityId)
				.OrderBy(x => x.CreationTime).Last();
				var xml = new DiadocXmlHelper(commentEntity);
				var commentData = xml.GetValue("Файл/Документ/СвУведУточ/ТекстУведУточ");
				Assert.AreEqual(comment, commentData);
				var torg12Rejected = invoiceiditem.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationRejected;
				Assert.IsTrue(torg12Rejected);
			});

			Wait();
		}

		void SignRevocation_Document()
		{
			Wait();

			var nonformid = "";
			var torg12Id = "";
			var invoiceid = "";

			//Неформализированный
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.DocumentType == Diadoc.Api.Proto.DocumentType.Nonformalized &&
				f.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RequestsMyRevocation);
				nonformid = ddkIndex.CurrentItem.Value.Entity.EntityId;
			});

			Wait();
			AsyncClick("Sign");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => {
				var nonformitem = ddkIndex.Items.Value.First(e => e.Entity.EntityId == nonformid);
				var nonformRevocationSigned = nonformitem.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationAccepted;
				Assert.IsTrue(nonformRevocationSigned);
			});

			Wait();

			//ТОРГ-12
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12 &&
				f.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RequestsMyRevocation);
				torg12Id = ddkIndex.CurrentItem.Value.Entity.EntityId;
			});

			Wait();
			AsyncClick("Sign");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			Wait();
			Click("Save");
			Wait();

			dispatcher.Invoke(() => {
				var torg12item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == torg12Id);
				var torg12RevocationSigned = torg12item.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationAccepted;
				Assert.IsTrue(torg12RevocationSigned);
			});

			Wait();

			//Инвойс
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Invoice &&
				f.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RequestsMyRevocation);
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
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Nonformalized);
			});
			Wait();
			dispatcher.Invoke(() => {
				var result = ddkIndex.Open().GetEnumerator();
				var open = Next<OpenResult>(result);
				Assert.IsTrue(File.Exists(open.Filename), open.Filename);
				var text = File.ReadAllText(open.Filename, Encoding.Default);
				Assert.IsTrue(text.Length > 0);
			});

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
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Nonformalized);
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
				Assert.True(text.Length > 0);
			});
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
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Nonformalized);
			});
			Wait();
			dispatcher.Invoke(() => {
				var result = ddkIndex.PrintItem().GetEnumerator();
				var task = Next<TaskResult>(result);
				task.Task.Wait();
				var taskResult = (task.Task as Task<string>).Result;
				Assert.True(File.Exists(taskResult));
				Assert.True(File.ReadAllBytes(taskResult).Length > 0);
			});

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

			var nonformid = "";
			var torg12id = "";
			var invoiceid = "";
			var comment = "НА СОГЛАСОВНИЕ";
			var comment2 = "СОГЛАСОВНИЕ ПОДТВЕРЖДЕНИЕ";

			// Неформализировнный
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Nonformalized &&
				f.Entity.DocumentInfo.NonformalizedDocumentMetadata.Status == NonformalizedDocumentStatus.InboundWaitingForRecipientSignature &&
				f.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationStatusNone &&
				(f.Entity.DocumentInfo.ResolutionStatus == null ||
				f.Entity.DocumentInfo.ResolutionStatus?.StatusType == ResolutionStatusType.None));
				nonformid = ddkIndex.CurrentItem.Value.Entity.EntityId;
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

			// Тогр-12
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

			// Инвойс
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

			// проверяем статус и подтвержадем согласование неформализированный
			dispatcher.Invoke(() => {
				var item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == nonformid);
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
				var item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == nonformid);
				var resreqEntt = item.Message.Entities.First(e => e.ResolutionInfo != null);
				Assert.IsTrue(resreqEntt.ResolutionInfo.ResolutionType == Diadoc.Api.Proto.Events.ResolutionType.Approve);
				var commentData =  Encoding.UTF8.GetString(resreqEntt.Content?.Data ?? new byte[0]);
				Assert.AreEqual(comment2, commentData);
			});

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

			var nonformid = "";
			var torg12id = "";
			var invoiceid = "";
			var comment = "НА СОГЛАСОВНИЕ";
			var comment2 = "СОГЛАСОВНИЕ ОТКАЗ";

			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Nonformalized &&
				f.Entity.DocumentInfo.NonformalizedDocumentMetadata.Status == NonformalizedDocumentStatus.InboundWaitingForRecipientSignature &&
				f.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationStatusNone &&
				(f.Entity.DocumentInfo.ResolutionStatus == null ||
				f.Entity.DocumentInfo.ResolutionStatus?.StatusType == ResolutionStatusType.None));
				nonformid = ddkIndex.CurrentItem.Value.Entity.EntityId;
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

			// проверяем статус и отказ в согласовании неформализированный
			dispatcher.Invoke(() => {
				var item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == nonformid);
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
				var item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == nonformid);
				var resreqEntt = item.Message.Entities.First(e => e.ResolutionInfo != null);
				Assert.IsTrue(resreqEntt.ResolutionInfo.ResolutionType == Diadoc.Api.Proto.Events.ResolutionType.Disapprove);
				var commentData =  Encoding.UTF8.GetString(resreqEntt.Content?.Data ?? new byte[0]);
				Assert.AreEqual(comment2, commentData);
			});

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

			var nonformid = "";
			var torg12id = "";
			var comment = "НА ПОДПИСЬ";

			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.DocumentType == Diadoc.Api.Proto.DocumentType.Nonformalized &&
				f.Entity.DocumentInfo.NonformalizedDocumentMetadata.Status == NonformalizedDocumentStatus.InboundWaitingForRecipientSignature &&
				f.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RevocationStatusNone &&
				(f.Entity.DocumentInfo.ResolutionStatus == null ||
				f.Entity.DocumentInfo.ResolutionStatus?.StatusType == ResolutionStatusType.None));
				nonformid = ddkIndex.CurrentItem.Value.Entity.EntityId;
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

			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12 &&
				f.Entity.DocumentInfo.XmlTorg12Metadata.Status == BilateralStatus.InboundWaitingForRecipientSignature &&
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

			// проверяем статус и подтвержадем подпись неформализированный
			dispatcher.Invoke(() => {
				var item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == nonformid);
				var resreqEntt = item.Message.Entities.First(e => e.ResolutionRequestInfo != null &&
				e.ResolutionRequestInfo.RequestType == Diadoc.Api.Proto.Events.ResolutionRequestType.SignatureRequest);
				Assert.IsTrue(resreqEntt.ResolutionRequestInfo.RequestType == Diadoc.Api.Proto.Events.ResolutionRequestType.SignatureRequest);
				var commentData =  Encoding.UTF8.GetString(resreqEntt.Content?.Data ?? new byte[0]);
				Assert.AreEqual(comment, commentData);
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
				var item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == nonformid);
				Assert.IsTrue(item.Entity.DocumentInfo.NonformalizedDocumentMetadata.DocumentStatus == NonformalizedStatus.InboundWithRecipientSignature);
			});

			Wait();

			// проверяем статус и подтвержадем подпись торг12
			dispatcher.Invoke(() => {
				var item = ddkIndex.Items.Value.First(e => e.Entity.EntityId == torg12id);
				var resreqEntt = item.Message.Entities.First(e => e.ResolutionRequestInfo != null &&
				e.ResolutionRequestInfo.RequestType == Diadoc.Api.Proto.Events.ResolutionRequestType.SignatureRequest);
				Assert.IsTrue(resreqEntt.ResolutionRequestInfo.RequestType == Diadoc.Api.Proto.Events.ResolutionRequestType.SignatureRequest);
				var commentData =  Encoding.UTF8.GetString(resreqEntt.Content?.Data ?? new byte[0]);
				Assert.AreEqual(comment, commentData);
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
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.Nonformalized);
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
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == DocumentType.XmlTorg12);
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
				Assert.AreEqual(itemsCount - 3, itemsCount1);
			});

			Wait();
		}

		public void DeleteAll_Documents()
		{
			foreach(var item in ddkIndex.Items)
			{
				dispatcher.Invoke(() => {ddkIndex.CurrentItem.Value = item;});
				Wait();
				AsyncClick("Delete");
				Wait();
				WaitWindow("АналитФАРМАЦИЯ");
				Wait();
				Click("Save");
				Wait();
			}
		}
	}
	*/
}
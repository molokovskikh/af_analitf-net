using System;
using System.Configuration;
using System.IO;
using System.Linq;
using Diadoc.Api;
using Diadoc.Api.Cryptography;
using Diadoc.Api.Proto.Events;
using NHibernate;
using Diadoc.Api.Proto.Invoicing;
using System.Text;
using AnalitF.Net.Client.Models;
using NHibernate.Linq;
using System.Security.Cryptography.X509Certificates;
using Diadoc.Api.Proto;
using System.Collections.Generic;
using Diadoc.Api.Proto.Documents;
using System.Threading;
using AnalitF.Net.Client.ViewModels.Diadok;
using Diadoc.Api.Com;
using DocumentType = Diadoc.Api.Com.DocumentType;
using RevocationStatus = Diadoc.Api.Proto.Documents.RevocationStatus;
using AttachmentType = Diadoc.Api.Proto.Events.AttachmentType;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class CreateDiadokInbox
	{
		public static class ddkConfig
		{
			public static string reciever_login = "f816686@mvrht.com";
			public static string reciever_passwd = "A123456";
			public static string reciever_boxid = "92c4c6b0948d4252b2b81c2b5730b5d1@diadoc.ru";
			public static string reciever_inn = "9698754923";

			public static string sender_login = "pdh23916@zasod.com";
			public static string sender_passwd = "A123456";
			public static string sender_boxid = "ebc25f997551449282541b8a6d1605c9@diadoc.ru";
			public static string sender_inn = "9656351023";
		}

		public void Execute(ISession session)
		{
			api = new DiadocApi(/*ConfigurationManager.AppSettings["DiadokApi"]*/"Analit-988b9e85-1b8e-40a9-b6bd-543790d0a7ec",
				"https://diadoc-api.kontur.ru", new WinApiCrypt());
			token = api.Authenticate(ddkConfig.sender_login, ddkConfig.sender_passwd);

			box = api.GetMyOrganizations(token).Organizations[0].Boxes[0];
			signers = new List<Signer>();
			var msgs = GetMessages();
			for(int i = 0; i < msgs.Item1.Count(); i++)
			{
				api.Delete(token, box.BoxId, msgs.Item2[i].MessageId, msgs.Item1[i].EntityId);
			}

			var msg = new MessageToPost();

			NonformalizedAttachment nfa = new NonformalizedAttachment();
			nfa.SignedContent = new SignedContent();
			nfa.SignedContent.Content = Encoding.GetEncoding(1251).GetBytes("ТЕСТОВЫЙ НЕФОРМАЛИЗИРОВННЫЙ ДОКУМЕНТ");
			nfa.SignedContent.SignWithTestSignature = true;
			nfa.FileName = "НеформализированныйДокумент.txt";
			nfa.NeedRecipientSignature = true;
			nfa.DocumentDate = DateTime.UtcNow.ToString("dd.MM.yyyy");
			nfa.DocumentNumber = DateTime.UtcNow.Millisecond.ToString();

			msg.NonformalizedDocuments.Add(nfa);
			msg.NonformalizedDocuments.Add(nfa);
			msg.NonformalizedDocuments.Add(nfa);
			msg.NonformalizedDocuments.Add(nfa);
			msg.NonformalizedDocuments.Add(nfa);
			msg.NonformalizedDocuments.Add(nfa);
			msg.NonformalizedDocuments.Add(nfa);

			XmlDocumentAttachment sii = new XmlDocumentAttachment();
			byte[] content = Encoding.GetEncoding(1251).GetBytes(DiadokFixtureData.InvoiceXml);
			byte[] sign = null;
			InvoiceInfo ii = api.ParseInvoiceXml(content);
			signers.Add(ii.Signer);
			GeneratedFile iiFile = api.GenerateInvoiceXml(token, ii);
			sii.SignedContent = new SignedContent();
			sii.SignedContent.SignWithTestSignature = true;
			sii.SignedContent.Content = iiFile.Content;
			sii.SignedContent.Signature = sign;

			msg.Invoices.Add(sii);
			msg.Invoices.Add(sii);
			msg.Invoices.Add(sii);
			msg.Invoices.Add(sii);
			msg.Invoices.Add(sii);
			msg.Invoices.Add(sii);
			msg.Invoices.Add(sii);
			msg.Invoices.Add(sii);

			XmlDocumentAttachment att12 = new XmlDocumentAttachment();
			content = Encoding.GetEncoding(1251).GetBytes(DiadokFixtureData.Torg12Xml);
			Torg12SellerTitleInfo tsti12 = api.ParseTorg12SellerTitleXml(content);
			signers.Add(tsti12.Signer);
			iiFile = api.GenerateTorg12XmlForSeller(token, tsti12, true);
			att12.SignedContent = new SignedContent();
			att12.SignedContent.SignWithTestSignature = true;
			att12.SignedContent.Content = iiFile.Content;
			att12.SignedContent.Signature = sign;

			msg.AddXmlTorg12SellerTitle(att12);
			msg.AddXmlTorg12SellerTitle(att12);
			msg.AddXmlTorg12SellerTitle(att12);
			msg.AddXmlTorg12SellerTitle(att12);
			msg.AddXmlTorg12SellerTitle(att12);
			msg.AddXmlTorg12SellerTitle(att12);
			msg.AddXmlTorg12SellerTitle(att12);
			msg.AddXmlTorg12SellerTitle(att12);

			msg.FromBoxId = ddkConfig.sender_boxid;
			msg.ToBoxId = ddkConfig.reciever_boxid;
			// пакет из трех
			api.PostMessage(token, msg);

			Thread.Sleep(TimeSpan.FromSeconds(3));

			msg.NonformalizedDocuments.Clear();
			msg.Invoices.Clear();
			msg.XmlTorg12SellerTitles.Clear();
			// инвойс
			msg.Invoices.Add(sii);
			api.PostMessage(token, msg);

			Thread.Sleep(TimeSpan.FromSeconds(3));

			msg.Invoices.Clear();
			msg.XmlTorg12SellerTitles.Clear();
			//  накладная
			msg.AddXmlTorg12SellerTitle(att12);
			api.PostMessage(token, msg);

		}

		public void Revocation(Message revocation)
		{
			RevocationRequestInfo revinfo = new RevocationRequestInfo();
			revinfo.Comment = "АННУЛИРОВНИЕ";
			revinfo.Signer = signers.First();

			var document = revocation.Entities.First();

			MessagePatchToPost patch = new MessagePatchToPost()
			{
				BoxId = box.BoxId,
				MessageId = revocation.MessageId
			};

			GeneratedFile revocationXml = api.GenerateRevocationRequestXml(
				token,
				box.BoxId,
				revocation.MessageId,
				document.EntityId,
				revinfo);

			SignedContent revocSignContent = new SignedContent();
			revocSignContent.Content = revocationXml.Content;
			revocSignContent.SignWithTestSignature = true;

			RevocationRequestAttachment revattch = new RevocationRequestAttachment();
			revattch.ParentEntityId = document.EntityId;
			revattch.SignedContent = revocSignContent;

			patch.AddRevocationRequestAttachment(revattch);

			api.PostMessagePatch(token, patch);
		}

		public Tuple<List<Document>, List<Message>> GetMessages()
		{
			if(string.IsNullOrEmpty(token))
				throw new Exception("token");
			Box box = api.GetMyOrganizations(token).Organizations[0].Boxes[0];

			var docs = api.GetDocuments(token, new DocumentsFilter {
				FilterCategory = "Any.Outbound",
				BoxId = box.BoxId,
				SortDirection = "Descending"
			}).Documents.ToList();
			var msgs = docs.Select(x => new { x.MessageId , x.EntityId}).Select(x => api.GetMessage(token, box.BoxId, x.MessageId, x.EntityId))
				.OrderByDescending(x => x.Timestamp).ToList();

			return Tuple.Create(docs, msgs);
		}

		public void OutBoundInvoices(List<Message> msgs)
		{
			var invoicesReq = msgs.Where(x => x.Entities[0].AttachmentType == AttachmentType.Invoice);

			foreach(var msg in invoicesReq)
			{
				Entity invoice = msg.Entities.FirstOrDefault(i => i.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.Invoice &&
				i.DocumentInfo.RevocationStatus == RevocationStatus.RevocationStatusNone);
				if(invoice == null)
					continue;
				Entity invoiceReciept = msg.Entities.FirstOrDefault(i => i.ParentEntityId == invoice.EntityId &&
				i.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.InvoiceConfirmation);

				GeneratedFile invoiceReceipt = api.GenerateInvoiceDocumentReceiptXml(
					token,
					box.BoxId,
					invoice.DocumentInfo.MessageId,
					invoiceReciept.EntityId,
					signers.First());

				SignedContent signContentInvoiceReciept = new SignedContent();
				signContentInvoiceReciept.Content = invoiceReceipt.Content;
				signContentInvoiceReciept.SignWithTestSignature = true;

				ReceiptAttachment receiptInvoice = new ReceiptAttachment {
					ParentEntityId = invoiceReciept.EntityId,
					SignedContent = signContentInvoiceReciept
				};

				var patch = new MessagePatchToPost {
					BoxId = box.BoxId,
					MessageId = invoice.DocumentInfo.MessageId
				};
				patch.AddReceipt(receiptInvoice);

				api.PostMessagePatch(token, patch);

				Thread.Sleep(1000);
			}
		}

		DiadocApi api;
		string token;
		Box box;
		List<Signer> signers;
	}
}
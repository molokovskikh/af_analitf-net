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
		public void Execute(ISession session)
		{
			api = new DiadocApi(/*ConfigurationManager.AppSettings["DiadokApi"]*/"Analit-988b9e85-1b8e-40a9-b6bd-543790d0a7ec",
				"https://diadoc-api.kontur.ru", new WinApiCrypt());
			token = api.Authenticate(ddk.ch_login, ddk.ch_passwd);

			box = api.GetMyOrganizations(token).Organizations[0].Boxes[0];
			signers = new List<Signer>();
			var msgs = GetMessages();
			for(int i = 0; i < msgs.Item1.Count(); i++)
			{
				api.Delete(token, box.BoxId, msgs.Item2[i].MessageId, msgs.Item1[i].EntityId);
			}

			var msg = new MessageToPost();

			CertEx cert = DiadokFixtureData.Cert;

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

			msg.FromBoxId = DiadokFixtureData.Sender_BoxId;
			msg.ToBoxId = DiadokFixtureData.Receiver_BoxId;
			// пакет из двух
			api.PostMessage(token, msg);

			Thread.Sleep(TimeSpan.FromSeconds(3));

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
			/*
			Thread.Sleep(30000);

			var messages = GetMessages();
			
			var torg12 = messages.Item2.Where(x => x.Entities[0].AttachmentType == AttachmentType.XmlTorg12 && 
			x.Entities[0].DocumentInfo.XmlTorg12Metadata.Status == BilateralDocumentStatus.InboundWithRecipientSignature &&
			x.Entities[0].DocumentInfo.RevocationStatus != RevocationStatus.RevocationStatusNone).First();

			var invoice = messages.Item2.Where(x => x.Entities[0].AttachmentType == AttachmentType.Invoice && 
			x.Entities[0].DocumentInfo.InvoiceMetadata.Status == InvoiceStatus.InboundFinished &&
			x.Entities[0].DocumentInfo.RevocationStatus != RevocationStatus.RevocationStatusNone).First();

			Revocation(torg12);
			Revocation(invoice);
			*/
		}

		public void Revocation(Message revocation)
		{
			MessagePatchToPost patch = new MessagePatchToPost();
			RevocationRequestInfo revinfo = new RevocationRequestInfo();
			revinfo.Comment = "АННУЛИРОВНИЕ";
			revinfo.Signer = signers.First();

			var document = revocation.Entities.First();

			GeneratedFile revocationXml = api.GenerateRevocationRequestXml(
				token,
				ddk.ie_boxid,
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
			Box box = box = api.GetMyOrganizations(token).Organizations[0].Boxes[0];
			var docs = api.GetDocuments(token, new DocumentsFilter {
				FilterCategory = "Any.Outbound",
				BoxId = box.BoxId,
				SortDirection = "Descending"
			}).Documents.ToList();
			var msgs = docs.Select(x => new { x.MessageId , x.EntityId}).Select(x => api.GetMessage(token, box.BoxId, x.MessageId, x.EntityId))
				.OrderByDescending(x => x.Timestamp).ToList();
			return Tuple.Create(docs, msgs);
		}

		DiadocApi api;
		string token;
		Box box;
		List<Signer> signers;
	}
}
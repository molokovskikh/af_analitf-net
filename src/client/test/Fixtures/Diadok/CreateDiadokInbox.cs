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

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class CreateDiadokInbox
	{
		public void Execute(ISession session)
		{
			api = new DiadocApi(/*ConfigurationManager.AppSettings["DiadokApi"]*/"Analit-988b9e85-1b8e-40a9-b6bd-543790d0a7ec",
				"https://diadoc-api.kontur.ru", new WinApiCrypt());
			token = api.Authenticate("c963977@mvrht.com", "222852");

			Box box = box = api.GetMyOrganizations(token).Organizations[0].Boxes[0];

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
			GeneratedFile iiFile = api.GenerateInvoiceXml(token, ii);
			sii.SignedContent = new SignedContent();
			sii.SignedContent.SignWithTestSignature = true;
			sii.SignedContent.Content = iiFile.Content;
			sii.SignedContent.Signature = sign;
			msg.FromBoxId = DiadokFixtureData.Sender_BoxId;
			msg.ToBoxId = DiadokFixtureData.Receiver_BoxId;

			msg.Invoices.Add(sii);

			XmlDocumentAttachment att12 = new XmlDocumentAttachment();
			content = Encoding.GetEncoding(1251).GetBytes(DiadokFixtureData.Torg12Xml);
			Torg12SellerTitleInfo tsti12 = api.ParseTorg12SellerTitleXml(content);
			iiFile = api.GenerateTorg12XmlForSeller(token, tsti12, true);
			att12.SignedContent = new SignedContent();
			att12.SignedContent.SignWithTestSignature = true;
			att12.SignedContent.Content = iiFile.Content;
			att12.SignedContent.Signature = sign;
			msg.FromBoxId = DiadokFixtureData.Sender_BoxId;
			msg.ToBoxId = DiadokFixtureData.Receiver_BoxId;

			msg.AddXmlTorg12SellerTitle(att12);
			// пакет из двух
			api.PostMessage(token, msg);

			msg.XmlTorg12SellerTitles.Clear();
			// два инвойса
			Thread.Sleep(TimeSpan.FromSeconds(10));
			api.PostMessage(token, msg);
			Thread.Sleep(TimeSpan.FromSeconds(10));
			api.PostMessage(token, msg);

			msg.Invoices.Clear();
			msg.AddXmlTorg12SellerTitle(att12);
			// две накладных
			Thread.Sleep(TimeSpan.FromSeconds(10));
			api.PostMessage(token, msg);
			Thread.Sleep(TimeSpan.FromSeconds(10));
			api.PostMessage(token, msg);
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
			var msgs = docs.Select(x => x.MessageId).Select(x => api.GetMessage(token, box.BoxId, x))
				.OrderByDescending(x => x.Timestamp).ToList();
			return Tuple.Create(docs, msgs);
		}

		DiadocApi api;
		string token;

	}
}
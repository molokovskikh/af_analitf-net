using System;
using System.Linq;
using NHibernate;
using NHibernate.Linq;
using Test.Support;
using Test.Support.Documents;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class Reject
	{
		public bool Local = false;
		public TestDocumentLog Document;
		public Service.Config.Config Config;

		public void Execute(ISession session)
		{
			var user = session.Query<TestUser>().First(u => u.Login == Environment.UserName);
			var supplier = user.GetActivePricesNaked(session).First().Price.Supplier;
			Document = new TestDocumentLog(supplier, user.AvaliableAddresses[0], "");
			Document.DocumentType = DocumentType.Reject;
			session.Save(Document);
			session.Save(new TestDocumentSendLog(user, Document));
			Document.CreateFile(Config.DocsPath, "test reject");
		}
	}
}
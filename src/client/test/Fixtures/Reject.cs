using System;
using System.Linq;
using AnalitF.Net.Client.Test.TestHelpers;
using NHibernate;
using NHibernate.Linq;
using Test.Support;
using Test.Support.Documents;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class Reject : ServerFixture
	{
		public TestDocumentLog Document;

		public override void Execute(ISession session)
		{
			var user = User(session);
			var supplier = user.GetActivePricesNaked(session).First().Price.Supplier;
			Document = new TestDocumentLog(supplier, user.AvaliableAddresses[0], "");
			Document.DocumentType = DocumentType.Reject;
			session.Save(Document);
			session.Save(new TestDocumentSendLog(user, Document));
			Document.CreateFile(Config.DocsPath, "test reject");
		}
	}
}
using System;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Test.TestHelpers;
using NHibernate;
using NHibernate.Linq;
using Test.Support;
using Test.Support.Documents;

namespace AnalitF.Net.Client.Test.Fixtures
{
	[Description("Создает накладную на сервере с файлом")]
	public class CreateWaybill : ServerFixture
	{
		public TestDocumentLog Document;
		public TestDocumentSendLog SendLog;
		public TestWaybill Waybill;
		public string Filename;

		private bool createFile;

		public CreateWaybill()
		{
			createFile = true;
		}

		public CreateWaybill(bool createFile = true)
		{
			this.createFile = createFile;
		}

		public override void Execute(ISession session)
		{
			var user = User(session);
			Waybill = Service.Test.TestHelpers.DataMother.CreateWaybill(session, user);
			Document = Waybill.Log;
			session.Save(Waybill);
			SendLog = new TestDocumentSendLog(user, Document);
			session.Save(SendLog);
			if (createFile)
				Filename = Waybill.Log.CreateFile(Config.DocsPath, "waybill content");
		}
	}
}
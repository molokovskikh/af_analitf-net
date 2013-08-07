using System;
using System.Linq;
using AnalitF.Net.Client.Test.TestHelpers;
using NHibernate;
using NHibernate.Linq;
using Test.Support;
using Test.Support.Documents;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class LoadWaybill
	{
		public bool Local = false;
		public Service.Config.Config Config;
		public TestDocumentLog Document;
		public TestDocumentSendLog SendLog;
		public string Filename;

		public void Execute(ISession session)
		{
			var user = session.Query<TestUser>().First(u => u.Login == Environment.UserName);
			var waybill = DataMother.CreateWaybill(session, user);
			var log = waybill.Log;
			session.Save(waybill);
			SendLog = new TestDocumentSendLog(user, log);
			session.Save(SendLog);
			Filename = waybill.Log.CreateFile(Config.DocsPath, "waybill content");
		}
	}
}
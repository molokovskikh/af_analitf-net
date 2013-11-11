﻿using System;
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
		public TestWaybill Waybill;
		public string Filename;

		private bool createFile;

		public LoadWaybill()
		{
		}

		public LoadWaybill(bool createFile = true)
		{
			this.createFile = createFile;
		}

		public void Execute(ISession session)
		{
			var user = session.Query<TestUser>().First(u => u.Login == Environment.UserName);
			Waybill = DataMother.CreateWaybill(session, user);
			var log = Waybill.Log;
			session.Save(Waybill);
			SendLog = new TestDocumentSendLog(user, log);
			session.Save(SendLog);
			if (!createFile)
				Filename = Waybill.Log.CreateFile(Config.DocsPath, "waybill content");
		}
	}
}
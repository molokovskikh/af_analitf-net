﻿using System.Linq;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.MySql;
using Common.NHibernate;
using NHibernate;
using NHibernate.Linq;
using Test.Support;
using Test.Support.Catalog;
using Test.Support.Documents;
using Test.Support.log4net;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class RejectedWaybill : ServerFixture
	{
		public TestWaybill Doc;

		public override void Execute(ISession session)
		{
			var reject = session.Query<TestReject>().First(r => r.Product != null && r.Producer != null
				&& r.CancelDate == null);
			Doc = DataMother.CreateWaybill(session, User(session));
			Doc.ProviderDocumentId = "reject";
			Doc.Lines.Add(new TestWaybillLine(Doc) {
				Product = reject.Product.CatalogProduct.Name,
				CatalogProduct = reject.Product,
				Producer = reject.Producer.Name,
				ProducerId = (int?)reject.Producer.Id,
				Quantity = 10,
				SupplierCost = 100,
				SupplierCostWithoutNDS = 90,
				NDS = 10,
				SerialNumber = reject.Series
			});
			var log = Doc.Log;
			session.Save(Doc);
			var sendLog = new TestDocumentSendLog(User(session), log);
			session.Save(sendLog);
		}

		public override void Rollback(ISession session)
		{
			//var docs = session.Query<TestWaybill>().Where(l => l.ProviderDocumentId == "reject")
			//	.Select(d => session.Query<TestDocumentSendLog>().First(l => l.Document == d.Log && l.ForUser == User(session)))
			//	.ToArray();
			//session.DeleteEach(docs);
		}
	}
}
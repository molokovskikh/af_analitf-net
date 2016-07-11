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
	[Description("Создает накладную с розничной ценой, для тестирования функции транспорта розничной цены")]
	public class CreateWaybillWithServerCost : ServerFixture
	{
		public override void Execute(ISession session)
		{
			var user = User(session);
			var waybill = Service.Test.TestHelpers.DataMother.CreateWaybill(session, user);
			var products = session.Query<TestProduct>().Where(x => !x.Hidden && x.CatalogProduct.Pharmacie).Take(1).ToArray();
			waybill.Lines.Clear();
			waybill.Lines.Add(new TestWaybillLine(waybill) {
				Product = products[0].FullName,
				CatalogProduct = products[0],
				Certificates = "РОСС BE.ФМ11.Д06711",
				CertificatesDate = "01.16.2013",
				Period = "30.09.2014",
				Producer = "Алкон-Куврер н.в. с.а.",
				Country = "БЕЛЬГИЯ",
				RetailCost = 600,
				RetailCostMarkup = 5,
				SupplierCostWithoutNDS = 536.17m,
				SupplierCost = 589.79m,
				Quantity = 1,
				SerialNumber = "A 565",
				Amount = 589.79m,
				NDS = 10,
				NDSAmount = 53.62m,
			});
			var document = waybill.Log;
			session.Save(waybill);
			var log = new TestDocumentSendLog(user, document);
			session.Save(log);
		}
	}

	[Description("Создает накладную для отчета Надб ЖНВЛС")]
	public class CreateWaybillForReport : ServerFixture
	{
		public TestDocumentLog Document;
		public TestDocumentSendLog SendLog;
		public TestWaybill Waybill;

		public override void Execute(ISession session)
		{
			var user = User(session);
			Waybill = Service.Test.TestHelpers.DataMother.CreateWaybill(session, user);
			Waybill.DocumentDate = DateTime.Now.AddYears(-1);
			Document = Waybill.Log;
			session.Save(Waybill);
			SendLog = new TestDocumentSendLog(user, Document);
			session.Save(SendLog);
		}
	}

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
using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class OrderRejectDetails : ViewModelFixture
	{
		private uint docId;
		private Client.ViewModels.OrderRejectDetails model;

		[SetUp]
		public void Setup()
		{
			var waybill = session.Query<Waybill>().First(r => r.DocType == DocType.Reject);
			docId = waybill.Id;
			model = Open(new Client.ViewModels.OrderRejectDetails(docId));
		}

		[Test]
		public void Show_details()
		{
			Assert.IsNotNull(model.Doc.Value);
			Assert.That(model.Lines.Value.Count, Is.GreaterThan(0));
			//для первой могу не найтись предложения
			model.CurrentLine.Value = model.Lines.Value.Skip(1).First(l => l.ProductId == null
				&& l.Product == "ПАПАВЕРИНА ГИДРОХЛОРИД супп. 20 мг N10");
			scheduler.Start();
			Assert.That(model.Offers.Value.Count, Is.GreaterThan(0), model.CurrentLine.Value.Product);

			model.CurrentLine.Value = model.Lines.Value.First(l => l.ProductId != null);
			scheduler.Start();
			Assert.That(model.Offers.Value.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Display_supplier_name()
		{
			model.CurrentLine.Value = model.Lines.Value.Skip(1).First(l => l.ProductId == null
				&& l.Product == "ПАПАВЕРИНА ГИДРОХЛОРИД супп. 20 мг N10");
			scheduler.Start();
			var saveSupplierName = model.Doc.Value.Supplier.FullName;
			model.Doc.Value.Supplier = null;
			Close(model);
			Open(model);
			Assert.AreEqual(model.DisplaySupplierName, saveSupplierName);
		}

		[Test]
		public void Mark_as_read()
		{
			var waybill = model.Session.Query<Waybill>().First(r => r.Id == docId);
			waybill.IsNew = true;
			model.Session.Flush();
			Assert.IsTrue(waybill.IsNew);
			Close(model);
			model = Open(new Client.ViewModels.OrderRejectDetails(docId));
			model.Session.Flush();
			waybill = model.Session.Query<Waybill>().First(r => r.Id == docId);
			Assert.IsTrue(!waybill.IsNew);
		}
	}
}
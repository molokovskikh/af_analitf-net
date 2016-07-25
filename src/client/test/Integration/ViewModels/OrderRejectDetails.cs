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
		[Test]
		public void Show_details()
		{
			var id = session.Query<Waybill>().First(w => w.DocType == DocType.Reject).Id;
			var model = new Client.ViewModels.OrderRejectDetails(id);
			Open(model);
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
			var id = session.Query<Waybill>().First(w => w.DocType == DocType.Reject).Id;
			var model = new Client.ViewModels.OrderRejectDetails(id);
			Open(model);
			model.CurrentLine.Value = model.Lines.Value.Skip(1).First(l => l.ProductId == null
				&& l.Product == "ПАПАВЕРИНА ГИДРОХЛОРИД супп. 20 мг N10");
			scheduler.Start();
			var saveSupplierName = model.Doc.Value.Supplier.FullName;
			model.Doc.Value.Supplier = null;
			Close(model);
			Open(model);
			Assert.AreEqual(model.DisplaySupplierName, saveSupplierName);
		}
	}
}
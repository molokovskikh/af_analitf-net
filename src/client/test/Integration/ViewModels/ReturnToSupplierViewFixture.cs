using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using NUnit.Framework;
using AnalitF.Net.Client.ViewModels.Inventory;
using AnalitF.Net.Client.Models.Results;
using System.IO;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using System.Collections.Generic;
using System;
using Common.NHibernate;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class ReturnToSupplierViewFixture : ViewModelFixture
	{
		private ReturnToSupplier doc;

		private ReturnToSuppliers model;

		private ReturnToSupplierDetails modelDetails;

		private Stock stock;

		[SetUp]
		public void Setup()
		{
			stock = new Stock()
			{
				Product = "Папаверин",
				Status = StockStatus.Available,
				Address = address,
				Quantity = 5,
				ReservedQuantity = 0
			};
			session.Save(stock);

			session.DeleteEach<ReturnToSupplier>();
			var supplier = session.Query<Supplier>().First();

			doc = new ReturnToSupplier
			{
				Date = DateTime.Now,
				Supplier = supplier,
				Address = address
			};
			session.Save(doc);

			model = Open(new ReturnToSuppliers());
			modelDetails = Open(new ReturnToSupplierDetails(doc.Id));
		}

		[Test]
		public void Export_returnToSupplier()
		{
			var result = (OpenResult)model.ExportExcel();
			Assert.IsTrue(File.Exists(result.Filename));
		}

		[Test]
		public void Export_ReturnToSupplierDetails_details()
		{
			var result = (OpenResult)modelDetails.ExportExcel();
			Assert.IsTrue(File.Exists(result.Filename));
		}

		[Test]
		public void Print_ReturnToSupplierDetails()
		{
			var results = modelDetails.Print().GetEnumerator();
			var preview = Next<DialogResult>(results);
			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		[Test]
		public void Print_ReturnToSupplierDetails_ReturnLabel()
		{
			var results = modelDetails.PrintReturnLabel().GetEnumerator();
			var preview = Next<DialogResult>(results);
			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		[Test]
		public void Print_ReturnToSupplierDetails_ReturnInvoice()
		{
			var results = modelDetails.PrintReturnInvoice().GetEnumerator();
			var preview = Next<DialogResult>(results);
			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		[Test]
		public void Print_ReturnToSupplierDetails_ReturnWaybill()
		{
			var results = modelDetails.PrintReturnWaybill().GetEnumerator();
			var preview = Next<DialogResult>(results);
			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		[Test]
		public void Print_ReturnToSupplierDetails_ReturnDivergenceAct()
		{
			var results = modelDetails.PrintReturnDivergenceAct().GetEnumerator();
			var preview = Next<DialogResult>(results);
			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		[Test]
		public void Doc_Flow()
		{
			//На складе есть Папаверин в количестве 5шт.
			Assert.AreEqual(stock.Quantity, 5);
			Assert.AreEqual(stock.ReservedQuantity, 0);

			//Мы создаем документ списание на 3 упаковки, после того как строка папаверина
			//добавлена и документ сохранен, на складе у нас будет - Папаверин 2шт, 3шт в резерве
			var line = new ReturnToSupplierLine(stock, 3);
			doc.Lines.Add(line);
			Assert.AreEqual(stock.Quantity, 2);
			Assert.AreEqual(stock.ReservedQuantity, 3);
			Assert.AreEqual(line.Quantity, 3);

			//Если мы закроем документ то получим - Папаверен 2шт, 0шт в резерве
			doc.Post(session);
			Assert.AreEqual(stock.Quantity, 2);
			Assert.AreEqual(stock.ReservedQuantity, 0);
			Assert.AreEqual(line.Quantity, 3);

			//Если мы снова откроем документ, то получим что было до закрытия - Папаверин 2шт, 3шт в резерве
			doc.UnPost(session);
			Assert.AreEqual(stock.Quantity, 2);
			Assert.AreEqual(stock.ReservedQuantity, 3);
			Assert.AreEqual(line.Quantity, 3);

			//Если документ будет удален то на складе получим - Папаверин 5шт, 0шт в резерве
			doc.BeforeDelete();
			Assert.AreEqual(stock.Quantity, 5);
			Assert.AreEqual(stock.ReservedQuantity, 0);
		}
	}
}

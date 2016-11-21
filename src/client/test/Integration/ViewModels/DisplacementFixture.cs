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
using System.Runtime.ExceptionServices;
using Common.NHibernate;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class DisplacementFixture : ViewModelFixture
	{
		private DisplacementDoc doc;

		private DisplacementDocs model;

		private EditDisplacementDoc modelDetails;

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
				ReservedQuantity = 0,
				SupplyQuantity = 5
			};
			session.Save(stock);

			session.DeleteEach<DisplacementDoc>();

			var dstAddress = new Address("Тестовый адрес");
			session.Save(dstAddress);

			doc = new DisplacementDoc {
				Date = DateTime.Now,
				Address = address,
				DstAddress = dstAddress
			};
			session.Save(doc);

			model = Open(new DisplacementDocs());
			modelDetails = Open(new EditDisplacementDoc(doc.Id));
		}

		[Test]
		public void Export_DisplacementDocs()
		{
			var result = (OpenResult)model.ExportExcel();
			Assert.IsTrue(File.Exists(result.Filename));
		}

		[Test]
		public void Export_DisplacementDoc()
		{
			var result = (OpenResult)modelDetails.ExportExcel();
			Assert.IsTrue(File.Exists(result.Filename));
		}

		[Test]
		public void Print_DisplacementDoc()
		{
			var results = modelDetails.Print().GetEnumerator();
			var preview = Next<DialogResult>(results);
			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		[Test]
		public void Print_DisplacementDocumentWaybill()
		{
			var results = modelDetails.PrintDisplacementDocumentWaybill().GetEnumerator();
			var preview = Next<DialogResult>(results);
			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		[Test]
		public void Print_DisplacementWaybill()
		{
			var results = modelDetails.PrintDisplacementWaybill().GetEnumerator();
			var preview = Next<DialogResult>(results);
			Assert.IsInstanceOf<RequirementWaybill>(preview.Model);
		}

		[Test]
		public void Print_PriceNegotiationProtocol()
		{
			var results = modelDetails.PrintPriceNegotiationProtocol().GetEnumerator();
			var preview = Next<DialogResult>(results);
			Assert.IsInstanceOf<RequirementNegotiationProtocol>(preview.Model);
		}


		[Test]
		public void Doc_Flow()
		{
			//На складе есть Папаверин в количестве 5шт.
			Assert.AreEqual(stock.Quantity, 5);
			Assert.AreEqual(stock.ReservedQuantity, 0);

			var dstStock = stock.Copy();
			dstStock.Address = doc.DstAddress;
			dstStock.Quantity = 0;
			session.Save(dstStock);

			//Мы создаем документ Внутренее перемещение на 3 упаковки, после того как строка папаверина
			//добавлена и документ сохранен, на складе у нас будет - Папаверин 2шт, 3шт в резерве
			// Резерв – состояние создания документа. Товар, введенный в документ, на складе находится в состоянии Резерв
			var line = new DisplacementLine(stock, dstStock, 3);
			doc.Lines.Add(line);
			session.Save(doc);
			session.Flush();
			Assert.AreEqual(stock.Quantity, 2);
			Assert.AreEqual(stock.ReservedQuantity, 3);
			Assert.AreEqual(dstStock.Quantity, 0);
			Assert.AreEqual(dstStock.ReservedQuantity, 0);
			Assert.AreEqual(line.Quantity, 3);
			Assert.AreEqual(doc.Status, DisplacementDocStatus.NotPosted);

			//Если мы закроем документ то получим - Папаверен 2шт, 0шт в резерве
			// В пути – документ закрыт и переведен. Товар на складе отправителя списан, на складе получателя находится в ожидаемом
			doc.Post(session);
			session.Save(doc);
			session.Flush();
			Assert.AreEqual(stock.Quantity, 2);
			Assert.AreEqual(stock.ReservedQuantity, 0);
			Assert.AreEqual(line.Quantity, 3);
			Assert.AreEqual(dstStock.Quantity, 0);
			Assert.AreEqual(dstStock.ReservedQuantity, 3);
			Assert.AreEqual(doc.Status, DisplacementDocStatus.Posted);

			// по факту получения товара на складе получателя, пользователь переводит документ в Получено. Товар на складе в состоянии В наличии
			doc.End(session);
			session.Save(doc);
			session.Flush();
			Assert.AreEqual(dstStock.Quantity, 3);
			Assert.AreEqual(dstStock.ReservedQuantity, 0);
			Assert.AreEqual(doc.Status, DisplacementDocStatus.End);

			// при откате - предыдущее состояние
			doc.ReEnd(session);
			session.Save(doc);
			session.Flush();
			Assert.AreEqual(dstStock.Quantity, 0);
			Assert.AreEqual(dstStock.ReservedQuantity, 3);
			Assert.AreEqual(doc.Status, DisplacementDocStatus.Posted);

			//Если мы снова откроем документ, то получим что было до закрытия - Папаверин 2шт, 3шт в резерве
			doc.UnPost(session);
			session.Save(doc);
			session.Flush();
			Assert.AreEqual(stock.Quantity, 2);
			Assert.AreEqual(stock.ReservedQuantity, 3);
			Assert.AreEqual(line.Quantity, 3);
			Assert.AreEqual(dstStock.SupplyQuantity, 5);
			Assert.AreEqual(doc.Status, DisplacementDocStatus.NotPosted);

			//Если документ будет удален то на складе получим - Папаверин 5шт, 0шт в резерве
			doc.BeforeDelete();
			session.Delete(doc);
			session.Flush();
			Assert.AreEqual(stock.Quantity, 5);
			Assert.AreEqual(stock.ReservedQuantity, 0);
		}
	}
}

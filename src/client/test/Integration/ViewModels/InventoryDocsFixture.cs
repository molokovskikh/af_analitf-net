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
using System;
using Common.NHibernate;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class InventoryDocsFixture : ViewModelFixture
	{
		private InventoryDoc doc;

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

			session.DeleteEach<ReturnDoc>();

			doc = new InventoryDoc
			{
				Date = DateTime.Now,
				Address = address
			};
			session.Save(doc);
		}

		[Test]
		public void Doc_Flow()
		{
			//На складе есть Папаверин в количестве 5 шт.
			Assert.AreEqual(stock.Quantity, 5);
			Assert.AreEqual(stock.SupplyQuantity, 5);

			var line = new InventoryLine(stock, 3, session);
			doc.Lines.Add(line);
			session.Save(doc);
			session.Flush();
			Assert.AreEqual(stock.Quantity, 5);
			Assert.AreEqual(stock.ReservedQuantity, 3);
			Assert.AreEqual(stock.SupplyQuantity, 5);
			Assert.AreEqual(line.Quantity, 3);

			doc.Post();
			session.Save(doc);
			session.Flush();
			Assert.AreEqual(stock.Quantity, 8);
			Assert.AreEqual(stock.ReservedQuantity, 0);
			Assert.AreEqual(stock.SupplyQuantity, 5);
			Assert.AreEqual(line.Quantity, 3);

			//Если мы снова откроем документ, то получим что было до закрытия - Папаверин 5 шт, 3 шт в поставке
			doc.UnPost();
			session.Save(doc);
			session.Flush();
			Assert.AreEqual(stock.Quantity, 5);
			Assert.AreEqual(stock.ReservedQuantity, 3);
			Assert.AreEqual(stock.SupplyQuantity, 5);
			Assert.AreEqual(line.Quantity, 3);

			//Если документ будет удален то на складе получим - Папаверин 5 шт, 0 шт в поставке
			doc.BeforeDelete(session);
			session.Delete(doc);
			session.Flush();
			Assert.AreEqual(stock.Quantity, 5);
			Assert.AreEqual(stock.ReservedQuantity, 0);
			Assert.AreEqual(stock.SupplyQuantity, 5);
		}

		// для стоков, заново создаваемых из каталога
		[Test]
		public void Doc_Flow_with_new_stock()
		{
			stock.Quantity = stock.ReservedQuantity = stock.SupplyQuantity = 0;
			stock.Status = StockStatus.InTransit;
			session.Update(stock);
			session.Flush();
			// новый пустой сток
			Assert.AreEqual(stock.Quantity, 0);
			Assert.AreEqual(stock.ReservedQuantity, 0);

			var line = new InventoryLine(stock, 3, session, true);
			doc.Lines.Add(line);
			session.Save(doc);
			session.Flush();
			Assert.AreEqual(stock.Quantity, 0);
			Assert.AreEqual(stock.ReservedQuantity, 3);
			Assert.AreEqual(stock.SupplyQuantity, 0);
			Assert.AreEqual(line.Quantity, 3);

			doc.Post();
			session.Save(doc);
			session.Flush();
			Assert.AreEqual(stock.Quantity, 3);
			Assert.AreEqual(stock.ReservedQuantity, 0);
			Assert.AreEqual(stock.SupplyQuantity, 0);
			Assert.AreEqual(line.Quantity, 3);
			Assert.AreEqual(stock.Status, StockStatus.Available);

			//Если мы снова откроем документ, то получим что было до закрытия
			doc.UnPost();
			session.Save(doc);
			session.Flush();
			Assert.AreEqual(stock.Quantity, 0);
			Assert.AreEqual(stock.ReservedQuantity, 3);
			Assert.AreEqual(stock.SupplyQuantity, 0);
			Assert.AreEqual(line.Quantity, 3);
			Assert.AreEqual(stock.Status, StockStatus.InTransit);

			//Если документ будет удален то на складе получим - Папаверин 5 шт, 0 шт в поставке
			doc.BeforeDelete(session);
			session.Delete(doc);
			session.Flush();
			Assert.AreEqual(stock.Quantity, 0);
			Assert.AreEqual(stock.ReservedQuantity, 0);
			Assert.AreEqual(stock.SupplyQuantity, 0);
		}
	}
}

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

		private InventoryDocs model;

		private EditInventoryDoc modelDetails;

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

			doc = new InventoryDoc
			{
				Date = DateTime.Now,
				Address = address
			};
			session.Save(doc);

			model = Open(new InventoryDocs());
			modelDetails = Open(new EditInventoryDoc(doc.Id));
		}

		[Test]
		public void Doc_Flow()
		{
			//Подготовлен(товар на складе отображается в Ожидаемом) и Проведен (товар на складе отображается В наличии)

			//На складе есть Папаверин в количестве 5 шт.
			Assert.AreEqual(stock.Quantity, 5);
			Assert.AreEqual(stock.SupplyQuantity, 0);

			//Мы создаем документ излишки на 3 упаковки, после того как строка папаверина
			//добавлена и документ сохранен, на складе у нас будет - Папаверин 5 шт, 3 шт в поставке
			var line = new InventoryDocLine(stock, 3, session);
			doc.Lines.Add(line);
			session.Save(doc);
			session.Flush();
			Assert.AreEqual(stock.Quantity, 5);
			Assert.AreEqual(stock.SupplyQuantity, 3);
			Assert.AreEqual(line.Quantity, 3);

			//Если мы закроем документ то получим - Папаверен 8 шт, 0 шт в поставке
			doc.Close();
			session.Save(doc);
			session.Flush();
			Assert.AreEqual(stock.Quantity, 8);
			Assert.AreEqual(stock.SupplyQuantity, 0);
			Assert.AreEqual(line.Quantity, 3);

			//Если мы снова откроем документ, то получим что было до закрытия - Папаверин 5 шт, 3 шт в поставке
			doc.ReOpen();
			session.Save(doc);
			session.Flush();
			Assert.AreEqual(stock.Quantity, 5);
			Assert.AreEqual(stock.SupplyQuantity, 3);
			Assert.AreEqual(line.Quantity, 3);

			//Если документ будет удален то на складе получим - Папаверин 5 шт, 0 шт в поставке
			doc.BeforeDelete(session);
			session.Delete(doc);
			session.Flush();
			Assert.AreEqual(stock.Quantity, 5);
			Assert.AreEqual(stock.SupplyQuantity, 0);
		}
	}
}

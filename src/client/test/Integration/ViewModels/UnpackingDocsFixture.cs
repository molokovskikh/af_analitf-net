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
	public class UnpackingDocsFixture : ViewModelFixture
	{
		private UnpackingDoc doc;

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
				RetailCost = 12.67m
			};
			session.Save(stock);

			session.DeleteEach<UnpackingDoc>();

			doc = new UnpackingDoc(address, user);
			session.Save(doc);
		}

		[Test]
		public void Doc_Flow()
		{
			//На складе есть Папаверин в количестве 5 шт.
			Assert.AreEqual(stock.Quantity, 5);

			// кратность 10
			var line = new UnpackingLine(stock, 10);
			doc.Lines.Add(line);
			session.Save(line);
			session.Save(doc);
			session.Flush();
			// после создания распаковки - распакованная упаковка в резерве
			Assert.AreEqual(stock.Quantity, 4);
			Assert.AreEqual(stock.ReservedQuantity, 1);
			Assert.AreEqual(line.SrcQuantity, 1);
			Assert.AreEqual(line.Quantity, 10);
			var dstStock = line.DstStock;
			Assert.AreEqual(dstStock.ReservedQuantity, 10);
			Assert.AreEqual(dstStock.Quantity, 10);
			Assert.AreEqual(line.RetailCost, 1.26m);
			Assert.AreEqual(dstStock.RetailCost, 1.26m);
			Assert.AreEqual(line.Delta, -0.07m);
			Assert.AreEqual(dstStock.Multiplicity, 10);
			Assert.IsTrue(dstStock.Unpacked); // распакованный
			Assert.IsFalse(stock.Unpacked); // нераспакованный

			// проводка: склад -1 целая, +10 распакованная
			doc.Post();
			session.Save(doc);
			session.Flush();
			Assert.AreEqual(stock.Quantity, 4);
			Assert.AreEqual(stock.ReservedQuantity, 0);
			Assert.AreEqual(dstStock.Quantity, 10);
			Assert.AreEqual(dstStock.ReservedQuantity, 0);
			Assert.IsFalse(line.Moved); // не было движения

			//Если снова откроем документ, то получим что было до закрытия
			doc.UnPost();
			session.Save(doc);
			session.Flush();
			Assert.AreEqual(stock.Quantity, 4);
			Assert.AreEqual(stock.ReservedQuantity, 1);
			Assert.AreEqual(dstStock.Quantity, 0);
			Assert.AreEqual(dstStock.ReservedQuantity, 10);

			//Если документ будет удален то на складе получим -Папаверин 5 шт
			doc.BeforeDelete();
			session.Delete(doc);
			session.Flush();
			Assert.AreEqual(stock.Quantity, 5);
			Assert.AreEqual(stock.ReservedQuantity, 0);
			Assert.AreEqual(dstStock.Quantity, 0);
			Assert.AreEqual(dstStock.ReservedQuantity, 0);
		}
	}
}

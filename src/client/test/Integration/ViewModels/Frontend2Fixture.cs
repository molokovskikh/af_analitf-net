using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnalitF.Net.Client.Test.TestHelpers;
using NUnit.Framework;
using AnalitF.Net.Client.ViewModels.Inventory;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models;
using Common.NHibernate;
using Common.Tools;
using AnalitF.Net.Client.Models.Results;
using NHibernate.Linq;
using System.Threading;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class Frontend2Fixture : ViewModelFixture
	{
		private Frontend2 model;

		private Stock stock;

		private BarcodeProducts BarcodeProduct;

		[SetUp]
		public void Setup()
		{
			settings.Waybills.Add(new WaybillSettings(user, address));
			session.DeleteEach<Stock>();
			session.DeleteEach<BarcodeProducts>();
			model = Open(new Frontend2());
			stock = new Stock()
			{
				Product = "Папаверин",
				Status = StockStatus.Available,
				Address = address,
				RetailCost = 1,
				Quantity = 5,
				ReservedQuantity = 0,
				Barcode = "10",
				ProductId = 1,
				Exp = SystemTime.Now()
			};
			stateless.Insert(stock);

			var product1 = GetProduct("АЦЕТИЛСАЛИЦИЛОВАЯ КИСЛОТА табл. 0.5 г N10");
			var stockForList = new Stock(session, product1, address, StockStatus.Available, 133)
			{
				Quantity = 5,
				Barcode = "4605635002748",
				Exp = SystemTime.Now()
			};
			stateless.Insert(stockForList);

			var product2 = GetProduct("АЦЕТИЛСАЛИЦИЛОВАЯ КИСЛОТА табл. 0.5г N20");
			stockForList = new Stock(session, product2, address, StockStatus.Available, 132)
			{
				Quantity = 5,
				Barcode = "4605635002748",
				Exp = SystemTime.Now()
			};
			stateless.Insert(stockForList);

			var products = new [] {
				GetProduct("АСПИРИН БАЙЕР табл. 100мг N20"),
				GetProduct("АСПИРИН БАЙЕР табл. 500 мг N10"),
				GetProduct("АСПИРИН БАЙЕР табл. 500 мг N10"),
			};
			for (int i = 0; i < 3; i++)
			{
				stockForList = new Stock(session, products[i], address, StockStatus.Available, 132)
				{
					Address = address,
					Quantity = 2 + i,
				};
				stateless.Insert(stockForList);
			}

			BarcodeProduct = new BarcodeProducts()
			{
				Product = product1,
				Producer = session.Query<Producer>().First(),
				Barcode = "30"
			};
			stateless.Insert(BarcodeProduct);

			session.DeleteEach<Check>();
			session.Flush();
		}

		private Product GetProduct(string name)
		{
			var catalog = session.Query<Catalog>().First(x => x.FullName == name);
			return session.Query<Product>().First(x => x.CatalogId == catalog.Id);
		}

		[Test]
		public void Doc_Close()
		{
			//На складе есть Папаверин в количестве 5шт.
			Assert.AreEqual(stock.Quantity, 5);

			//добавляем строку на 3 упаковки
			var line = new CheckLine(stock, 3);
			model.Lines.Add(line);
			model.checkType = CheckType.SaleBuyer;
			// Оплата по чеку
			var result = model.Close().GetEnumerator();
			result.MoveNext();
			var dialog = ((Checkout)((DialogResult)result.Current).Model);

			dialog.Amount.Value = 10;
			result.MoveNext();

			// после оплаты на складе остается 2
			var check = session.Query<Check>().First();
			var loadstock = session.Query<Stock>().Where(x => x.Id == stock.Id).First();
			Assert.AreEqual(loadstock.Quantity, 2);
			Assert.AreEqual(check.Sum, 3);
			Assert.AreEqual(check.Status, Status.Closed);
		}

		[Test]
		public void ReturnCheck()
		{
			//На складе есть Папаверин в количестве 5шт.
			Assert.AreEqual(stock.Quantity, 5);

			//добавляем строку на 3 упаковки
			var line = new CheckLine(stock, 3);
			model.Lines.Add(line);
			model.checkType = CheckType.SaleBuyer;
			// Оплата по чеку
			var result = model.Close().GetEnumerator();
			result.MoveNext();
			var dialog = ((Checkout)((DialogResult)result.Current).Model);

			dialog.Amount.Value = 10;
			result.MoveNext();
			// после оплаты на складе остается 2
			var check = session.Query<Check>().First();
			var loadstock = session.Query<Stock>().Where(x => x.Id == stock.Id).First();
			Assert.AreEqual(loadstock.Quantity, 2);
			Assert.AreEqual(check.Sum, 3);
			Assert.AreEqual(check.Status, Status.Closed);

			// Возврат
			var resultreturn = model.ReturnCheck().GetEnumerator();
			resultreturn.MoveNext();
			var dialogchecks = ((Checks)((DialogResult)resultreturn.Current).Model);
			dialogchecks.CurrentItem.Value = check;
			dialogchecks.DialogCancelled = false;
			resultreturn.MoveNext();

			result = model.Close().GetEnumerator();

			result.MoveNext();
			dialog = ((Checkout)((DialogResult)result.Current).Model);


			dialog.Amount.Value = 10;
			result.MoveNext();

			session.Clear();
			var returnstock = session.Query<Stock>().Where(x => x.Id == stock.Id).First();
			Assert.AreEqual(returnstock.Quantity, 5);
		}

		[Test]
		public void Sell_only_available_stock()
		{
			var product = GetProduct("АСПИРИН-С БАЙЕР табл. шип. N10");
			var transitStock = new Stock(session, product, address, StockStatus.InTransit);
			stateless.Insert(transitStock);
			CatalogChooser dialog = null;
			manager.DialogOpened.Subscribe(x => {
				dialog = (CatalogChooser)x;
				scheduler.Start();
			});
			model.SearchBehavior.ActiveSearchTerm.Value = "АСП";
			Assert.That(dialog.Items.Value.Count, Is.GreaterThan(0));
			Assert.That(dialog.Items.Value.Select(x => x.CatalogId), Does.Not.Contains(product.CatalogId));
		}

		[Test]
		public void FreeSale()
		{
			settings.FreeSale = true;
			var result = model.BarcodeScanned("30").GetEnumerator();
			result.MoveNext();
			var dialog = ((InputQuantityRetailCost)((DialogResult)result.Current).Model);
			dialog.Quantity.Value = 2;
			dialog.RetailCost.Value = 20;
			dialog.OK();
			result.MoveNext();
			Assert.AreEqual(model.CurrentLine.Value.BarcodeProduct, BarcodeProduct);

			var resultClose = model.Close().GetEnumerator();
			resultClose.MoveNext();
			var dialogClose = ((Checkout)((DialogResult)resultClose.Current).Model);
			dialogClose.Amount.Value = 10;
			resultClose.MoveNext();

			var check = session.Query<Check>().First();
			Assert.AreEqual(check.Sum, 40);
			Assert.AreEqual(check.Status, Status.Closed);
			Assert.AreEqual(check.Lines[0].Stock, null);
			Assert.AreEqual(check.Lines[0].BarcodeProduct, BarcodeProduct);

		}
	}
}
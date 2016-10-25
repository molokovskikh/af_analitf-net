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
using AnalitF.Net.Client.Helpers;
using Common.NHibernate;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class FrontendFixture : ViewModelFixture<Frontend>
	{
	 private uint count;

		private void AddFreshStockItem()
		{
			count = 0;
			session.DeleteEach<Stock>();
			AddStockItem();
		}

		private void AddStockItem()
		{
			count++;
			var stock = new Stock()
			{
				ProductId = count,
				Product = "Папаверин",
				Status = StockStatus.Available,
				Address = address,
				Quantity = 5,
				ReservedQuantity = 0,
				Barcode = "10",
				RetailCost = 100
			};
			session.Save(stock);
		}

		[Test]
		public void Find_by_barcode_scanned()
		{
			AddFreshStockItem();
			var barcode = "10";
			model.BarcodeScanned(barcode);
			Assert.AreEqual(false, model.HasError.Value);
			Assert.AreEqual("Штрих код", model.LastOperation.Value);
			Assert.AreEqual("Папаверин", model.CurrentLine.Value.Product);
		}

		[Test]
		public void Find_by_barcode()
		{
			AddFreshStockItem();
			var barcode = "10";
			model.Input.Value = barcode;
			model.Quantity = new NotifyValue<uint?>(1);
			model.SearchByBarcode();
			Assert.AreEqual(false, model.HasError.Value);
			Assert.AreEqual("Штрих код", model.LastOperation.Value);
			Assert.AreEqual("Папаверин", model.CurrentLine.Value.Product);
		}

		[Test]
		public void Find_by_id()
		{
			AddFreshStockItem();
			var id = 1;
			model.Input.Value = id.ToString();
			model.Quantity = new NotifyValue<uint?>(1);
			model.SearchByProductId();
			Assert.AreEqual(false, model.HasError.Value);
			Assert.AreEqual("Код товара", model.LastOperation.Value);
			Assert.AreEqual("Папаверин", model.CurrentLine.Value.Product);
		}

		[Test]
		public void Find_by_barcode_error()
		{
			AddFreshStockItem();
			var barcode = "11";
			model.Quantity = new NotifyValue<uint?>(1);
			model.Input.Value = barcode;
			model.SearchByBarcode();
			Assert.AreEqual(true, model.HasError.Value);
			Assert.AreEqual("Товар не найден", model.LastOperation.Value);
		}

		[Test]
		public void Find_by_barcode_error_quantity()
		{
			AddFreshStockItem();
			var barcode = "10";
			model.Input.Value = barcode;
			model.Quantity = new NotifyValue<uint?>(100);
			model.SearchByBarcode();
			Assert.AreEqual(true, model.HasError.Value);
			Assert.AreEqual("Нет требуемого количества", model.LastOperation.Value);
		}
	}
}

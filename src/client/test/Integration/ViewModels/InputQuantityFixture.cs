﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Inventory;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models;
using Common.NHibernate;
using AnalitF.Net.Client.Models.Results;
using Common.Tools;
using AnalitF.Net.Client.Test.Fixtures;
using NHibernate.Linq;
using System.Reactive.Linq;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class InputQuantityFixture : ViewModelFixture
	{
		private Frontend2 model;
		private Stock stock;
		private Catalog catalog;
		private CatalogName catalogName;
		[SetUp]
		public void Setup()
		{
			session.DeleteEach<Stock>();
			settings.Waybills.Add(new WaybillSettings(user, address));
			model = Open(new Frontend2());
		}

		[Test]
		public void Unpack()
		{
			stock = new OrderedStock()
			{
				Product = "Папаверин",
				Status = StockStatus.Available,
				Address = address,
				RetailCost = 1,
				Quantity = 10,
				ReservedQuantity = 0,
				Barcode = "10",
				ProductId = 1,
				CatalogId = 1,
				Exp = SystemTime.Now()
			};
			session.Save("AnalitF.Net.Client.Models.Inventory.Stock", stock);
			session.Flush();
			//добавляем строку на 3 упаковки
			var line = new CheckLine(stock, 3, CheckType.SaleBuyer);
			model.Lines.Add(line);
			model.CurrentLine.Value = line;

			var result = model.Unpack().GetEnumerator();
			result.MoveNext();
			var dialog = ((InputQuantity)((DialogResult)result.Current).Model);
			dialog.Quantity.Value = 2;
			dialog.Multiplicity.Value = 6;
			dialog.OK();
			result.MoveNext();

			// из них две в чеке
			Assert.AreEqual(model.CurrentLine.Value.Quantity, 2);
			var dstStock = model.CurrentLine.Value.Stock;

			result = model.Close().GetEnumerator();
			result.MoveNext();
			var dialog1 = ((Checkout)((DialogResult)result.Current).Model);
			dialog1.Amount.Value = 10;
			result.MoveNext();

			session.Clear();
			var loadstock = session.Query<Stock>().Where(x => x.Id == dstStock.Id).First();
			Assert.AreEqual(loadstock.Quantity, 6 - 2);
			Assert.AreEqual(loadstock.Multiplicity, 6);

		}
	}
}
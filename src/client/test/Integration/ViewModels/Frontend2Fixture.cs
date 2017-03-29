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

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class Frontend2Fixture : ViewModelFixture
	{
		private Frontend2 model;

		private Stock stock;

		[SetUp]
		public void Setup()
		{
			settings.Waybills.Add(new WaybillSettings(user, address));
			session.DeleteEach<Stock>();
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

			var stockForList = new Stock()
			{
				Product = "Аспирин 1",
				Status = StockStatus.Available,
				Address = address,
				RetailCost = 133,
				Quantity = 5,
				ReservedQuantity = 0,
				Barcode = "4605635002748",
				ProductId = 3,
				Exp = SystemTime.Now()
			};
			stateless.Insert(stockForList);

			stockForList = new Stock()
			{
				Product = "Аспирин 2",
				Status = StockStatus.Available,
				Address = address,
				RetailCost = 132,
				Quantity = 5,
				ReservedQuantity = 0,
				Barcode = "4605635002748",
				ProductId = 2,
				Exp = SystemTime.Now()
			};
			stateless.Insert(stockForList);

			for (int i = 0; i < 3; i++)
			{
				stockForList = new Stock()
				{
					Exp = SystemTime.Now().AddDays(-i),
					Product = $"Аспирин 0{i}",
					Status = StockStatus.Available,
					Address = address,
					RetailCost = 132,
					Quantity = 2 + i,
					ReservedQuantity = 0,
					Barcode = "4030855000890",
					ProductId = Convert.ToUInt32(4 + (i == 1 ? 0 : i)) //нужен один повтор
				};
				stateless.Insert(stockForList);
			}

			session.DeleteEach<Check>();
			session.Flush();
		}

		[Test]
		public void Doc_Close()
		{
			//На складе есть Папаверин в количестве 5шт.
			Assert.AreEqual(stock.Quantity, 5);

			//добавляем строку на 3 упаковки
			var line = new CheckLine(stock, 3);
			model.Lines.Add(line);

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
	}
}
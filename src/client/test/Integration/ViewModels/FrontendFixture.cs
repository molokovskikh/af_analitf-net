using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Test.TestHelpers;
using NUnit.Framework;
using AnalitF.Net.Client.ViewModels.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using Common.NHibernate;
using AnalitF.Net.Client.Models;
using Common.Tools;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class FrontendFixture : ViewModelFixture
	{
		private Frontend model;

		private Stock stock;

		[SetUp]
		public void Setup()
		{
			settings.Waybills.Add(new WaybillSettings(user, address));
			session.DeleteEach<Stock>();
			model = Open(new Frontend());
			stock = new Stock() {
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

			var stockForList = new Stock() {
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

			stockForList = new Stock() {
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

			for (int i = 0; i < 3; i++) {
				stockForList = new Stock() {
					Exp = SystemTime.Now().AddDays(- i),
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
		public void Find_by_barcode_scanned()
		{
			var barcode = "10";
			model.BarcodeScanned(barcode);
			Assert.AreEqual(false, model.HasError.Value);
			Assert.AreEqual("Штрих код", model.LastOperation.Value);
			Assert.AreEqual("Папаверин", model.CurrentLine.Value.Product);
		}

		[Test]
		public void Find_by_barcodeDubleMatch()
		{
			var barcode = "4605635002748";
			model.Input.Value = barcode;
			model.Quantity = new NotifyValue<uint?>(1);

			var result = model.SearchByTerm(barcode).GetEnumerator();
			result.MoveNext();
			var dialog = (StockSearch)((DialogResult)result.Current).Model;
			Open(dialog);
			Assert.IsTrue(dialog.Items.Value.Count == 2);
			Assert.IsTrue(dialog.Items.Value.Any(s=>s.Product == "Аспирин 1"));
			Assert.IsTrue(dialog.Items.Value.Any(s => s.Product == "Аспирин 2"));

			dialog.CurrentItem.Value = dialog.Items.Value.First();
			dialog.EnterItem();

			//у товаров разные ProductId, поэтому SearchByProductId должен дать один единственный остаток для "Аспирин 1", затем добавить его в чек
			Assert.AreEqual(false, model.HasError.Value);
			Assert.AreEqual(false, model.LastOperation.HasValue);
			model.Input.Value = dialog.CurrentItem.Value.ProductId.ToString();
			model.SearchByProductId();

			Assert.AreEqual(false, model.HasError.Value);
			Assert.AreEqual("Код товара", model.LastOperation.Value);
			Assert.AreEqual("Аспирин 1", model.CurrentLine.Value.Product);
		}


		[Test]
		public void Find_by_barcodeDubleMatchSameProductId()
		{
			var barcode = "4030855000890";
			model.Input.Value = barcode;
			model.Quantity = new NotifyValue<uint?>(1);

			var result = model.SearchByTerm(barcode).GetEnumerator();
			result.MoveNext();
			var dialog = (StockSearch) ((DialogResult) result.Current).Model;
			Open(dialog);
			Assert.AreEqual(3, dialog.Items.Value.Count);
			Assert.AreEqual("Поиск товара по названию", dialog.DisplayName);
			Assert.IsTrue(dialog.Items.Value.Any(s => s.Product == "Аспирин 01"));
			Assert.IsTrue(dialog.Items.Value.Any(s => s.Product == "Аспирин 02"));


			// выбор Barcode = "4030855000890"
			dialog.CurrentItem.Value = dialog.Items.Value.First();
			dialog.EnterItem();
			// Barcode = "10" старше, чем "4030855000890", поэтому после проверки он должен быть выбран по умолчанию
			Assert.AreEqual(2, dialog.Items.Value.Count);
			Assert.AreEqual(2, dialog.Items.Value.Count(s => s.ProductId == 4));
			Assert.IsTrue(dialog.Items.Value.Any(s => s.Barcode == "4030855000890"));
			Assert.IsTrue(dialog.Items.Value.Any(s => s.Barcode == "4030855000890"));
			Assert.IsTrue(dialog.Items.Value.Any(s => s.Product == "Аспирин 01"));
			Assert.IsTrue(dialog.Items.Value.Any(s => s.Product == "Аспирин 00"));
			Assert.AreEqual("4030855000890", dialog.CurrentItem.Value.Barcode);
			Assert.AreEqual(4, dialog.CurrentItem.Value.ProductId);
			Assert.AreEqual("Поиск товара: 4030855000890 - \"Аспирин 00\"", dialog.DisplayName);
		}


		[Test]
		public void Find_by_barcode()
		{
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
			var id = "1";
			model.Input.Value = id;
			model.Quantity = new NotifyValue<uint?>(1);
			model.SearchByProductId();
			Assert.AreEqual(false, model.HasError.Value);
			Assert.AreEqual("Код товара", model.LastOperation.Value);
			Assert.AreEqual("Папаверин", model.CurrentLine.Value.Product);
		}

		[Test]
		public void Find_by_barcode_error()
		{
			var barcode = "11";
			model.Quantity = new NotifyValue<uint?>(1);
			model.Input.Value = barcode;
			model.SearchByBarcode();
			Assert.AreEqual(true, model.HasError.Value);
			Assert.AreEqual("Товар не найден", model.LastOperation.Value);
		}

		[Test]
		public void Find_by_name()
		{
			var name = "Папаверин";
			model.Quantity = new NotifyValue<uint?>(1);
			model.Input.Value = name;
			var result = model.SearchByTerm().GetEnumerator();
			result.MoveNext();
			var dialog = (StockSearch)((DialogResult)result.Current).Model;
			Open(dialog);
			Assert.AreEqual(1, dialog.Items.Value.Count);
		}

		[Test]
		public void Find_by_cost()
		{
			var cost = "1";
			model.Quantity = new NotifyValue<uint?>(1);
			model.Input.Value = cost;
			var result = model.SearchByCost().GetEnumerator();
			result.MoveNext();
			var dialog = (StockSearch)((DialogResult)result.Current).Model;
			Open(dialog);
			Assert.AreEqual(1, dialog.Items.Value.Count);
		}

		[Test, Ignore("тест не актуален")]
		public void Doc_Close_SaleBuyer()
		{
			//На складе есть Папаверин в количестве 5шт.
			Assert.AreEqual(stock.Quantity, 5);

			//добавляем строку на 3 упаковки
			var line = new CheckLine(stock, 3, CheckType.SaleBuyer);
			model.Lines.Add(line);
			Assert.AreEqual(stock.Quantity, 2);

			// Оплата по чеку
			var result = model.Close().GetEnumerator();
			result.MoveNext();
			var dialog = ((Checkout)((DialogResult)result.Current).Model);
			dialog.Amount.Value = 10;
			result.MoveNext();

			// после оплаты на складе остается 2
			var check = session.Query<Check>().First();
			Assert.AreEqual(stock.Quantity, 2);
			Assert.AreEqual(check.Sum, 3);
			Assert.AreEqual(check.CheckType, CheckType.SaleBuyer);
		}

		[Test, Ignore("тест не актуален")]
		public void Doc_Close_CheckReturn()
		{
			//На складе есть Папаверин в количестве 5шт.
			Assert.AreEqual(stock.Quantity, 5);
			model.Trigger();

			//добавляем строку на 3 упаковки
			var line = new CheckLine(stock, 3, CheckType.CheckReturn);
			model.Lines.Add(line);
			Assert.AreEqual(stock.Quantity, 8);

			// Возврат по чеку
			var result = model.Close().GetEnumerator();
			result.MoveNext();

			// после возврата на складе остается 8
			var check = session.Query<Check>().First();
			Assert.AreEqual(stock.Quantity, 8);
			Assert.AreEqual(check.Sum, 3);
			Assert.AreEqual(check.CheckType, CheckType.CheckReturn);
		}

		[Test, Ignore("тест не актуален")]
		public void Unpack()
		{
			//добавляем строку на 3 упаковки
			var line = new CheckLine(stock, 3, CheckType.SaleBuyer);
			model.Lines.Add(line);
			model.CurrentLine.Value = line;
			Assert.AreEqual(stock.Quantity, 2);

			var result = model.Unpack().GetEnumerator();
			var dialog = Next<DialogResult>(result);
			var settings = (Frontend.UnpackSettings)((SimpleSettings)dialog.Model).Target;
			settings.Quantity = 2;
			settings.Multiplicity = 6;
			result.MoveNext();

			// было 5, одну распаковали
			Assert.AreEqual(stock.Quantity, 4);
			var dstStock = model.CurrentLine.Value.Stock;
			// распаковали на 6 частей
			Assert.AreEqual(dstStock.Multiplicity, 6);
			// из них две в чеке
			Assert.AreEqual(dstStock.Quantity, 6 - 2);
			Assert.AreEqual(model.CurrentLine.Value.Quantity, 2);
		}

		[Test]
		public void Switch_payment_type()
		{
			Assert.AreEqual(PaymentType.Cash, model.PaymentType.Value);
			Assert.AreEqual(DescriptionHelper.GetDescription(PaymentType.Cash), model.PaymentTypeName.Value);

			model.SwitchPaymentType();

			Assert.AreEqual(PaymentType.Cashless, model.PaymentType.Value);
			Assert.AreEqual(DescriptionHelper.GetDescription(PaymentType.Cashless), model.PaymentTypeName.Value);
		}

		[Test]
		public void Switch_check_type()
		{
			model.Trigger();
			var status = model.Status.Value == "Открыт чек продажи" ? "Открыт возврат по чеку" : "Открыт чек продажи";
			model.Trigger();
			Assert.AreEqual(status, model.Status.Value);
		}

		[Test]
		public void Check_status_after_adding_line()
		{
			model.Trigger();
			Assert.AreEqual("Открыт возврат по чеку", model.Status.Value);
			var line = new CheckLine(stock, 1, CheckType.CheckReturn);
			model.Lines.Add(line);
			Assert.AreEqual("Открыт возврат по чеку", model.Status.Value);
		}
	}
}

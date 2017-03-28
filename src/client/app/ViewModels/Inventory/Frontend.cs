using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Common.Tools;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Common.NHibernate;
using NHibernate.Linq;
using ReactiveUI;
using AnalitF.Net.Client.ViewModels.Dialogs;
using System.Windows;
using System.ComponentModel.DataAnnotations;
using NHibernate;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class Frontend : BaseScreen2
	{
		private const string TXT_START_STATUS = "Готов к работе (F1 для справки)";

		public Frontend()
		{
			DisplayName = "Регистрация продаж";
			InitFields();
			Lines = new ReactiveCollection<CheckLine>();
			Lines.Changed.Subscribe(_ => {
				if (Lines.Count == 0) {
					Discount.Value = null;
					Sum.Value = null;
				} else {
					Status.Value = StatusText();
					Change.Value = null;
					Sum.Value = Lines.Sum(x => x.RetailSum);
					Discount.Value = Lines.Sum(x => x.DiscontSum);
				}
			});
			Status.Value = TXT_START_STATUS;
			checkType = CheckType.SaleBuyer;
			PaymentType.Value = Models.Inventory.PaymentType.Cash;
			PaymentType.Subscribe(_ => {
				PaymentTypeName.Value = DescriptionHelper.GetDescription(PaymentType.Value);
			});
			Session.FlushMode = FlushMode.Never;
		}

		public NotifyValue<bool> HasError { get; set; }
		public NotifyValue<string> Status { get; set; }
		public NotifyValue<decimal?> Discount { get; set; }
		public NotifyValue<decimal?> Sum { get; set; }
		public NotifyValue<decimal?> Change { get; set; }
		public NotifyValue<uint?> Quantity { get; set; }
		public NotifyValue<string> Input { get; set; }
		public NotifyValue<string> LastOperation { get; set; }
		public NotifyValue<CheckLine> CurrentLine { get; set; }
		public ReactiveCollection<CheckLine> Lines { get; set; }
		public NotifyValue<string> PaymentTypeName { get; set; }

		private CheckType checkType { get; set; }
		public NotifyValue<PaymentType> PaymentType { get; set; }

		private string StatusText()
		{
			return checkType == CheckType.SaleBuyer ? "Открыт чек продажи" : "Открыт возврат по чеку";
		}

		private void Message(string text)
		{
			HasError.Value = false;
			Input.Value = null;
			LastOperation.Value = text;
		}

		private void Error(string message)
		{
			HasError.Value = true;
			Input.Value = null;
			LastOperation.Value = message;
		}

		// Отменить чек	Alt + Delete
		public void Cancel()
		{
			if (!Confirm("Отменить чек?"))
				return;
			Message("Отмена чека");
			Reset();
		}

		// Редактирование количества	Ctrl + Q
		public void UpdateQuantity()
		{
			var value = NullableConvert.ToUInt32(Input.Value);
			if (value == null) {
				Error("Ошибка ввода количества");
				return;
			}

			if (CurrentLine.Value == null) {
				Error("Строка не выбрана");
				return;
			}

			CurrentLine.Value.Quantity = value.Value;
			Message("Ввод количества");
		}

		// Перенести содержимое поля ввода в поле количество	* (NUM)
		public void InputQuantity()
		{
			var value = NullableConvert.ToUInt32(Input.Value);
			if (value == null) {
				Error("Ошибка ввода количества");
				return;
			}

			Message("Ввод количества");
			Quantity.Value = value.Value;
		}

		// Поиск по коду	F2
		public void SearchByProductId()
		{
			var id = NullableConvert.ToUInt32(Input.Value);
			if (id == null) {
				Error("Ошибка ввода кода");
				return;
			}
			var stock = Env.Query(s => Stock.AvailableStocks(s, Address).FirstOrDefault(x => x.ProductId == id)).Result;
			UpdateProduct(stock, "Код товара");
		}

		public void BarcodeScanned(string barcode)
		{
			if (Quantity.Value == null) {
				Quantity.Value = 1;
			}
			if (String.IsNullOrEmpty(barcode)) {
				Error("Ошибка ввода штрих-кода");
				return;
			}
			var stock = Env.Query(s => Stock.AvailableStocks(s, Address).Where(x => x.Barcode == barcode)).Result;
			UpdateProduct(stock, "Штрих код");
		}

		// Поиск по штрих-коду F3
		public void SearchByBarcode()
		{
			if (String.IsNullOrEmpty(Input.Value)) {
				Error("Ошибка ввода штрих-кода");
				return;
			}
			var stock = Env.Query(s => Stock.AvailableStocks(s, Address).Where(x => x.Barcode == Input.Value)).Result;
			UpdateProduct(stock, "Штрих код");
		}

		private bool HasStocksSimilarities(List<Stock> stocks)
		{
			var item = stocks.FirstOrDefault();
			if (item == null)
				return false;

			var anotherProductIdInList = stocks.Where(i => i.ProductId.HasValue)
				.Select(d => d.ProductId).GroupBy(f => f.Value).Count();

			var currenProductIdInStock = Env.Query(s => Stock
				.AvailableStocks(s, Address).Where(i => i.ProductId.HasValue)
				.Count(x => x.ProductId.Value == item.ProductId.Value)).Result;

			return anotherProductIdInList > 1 || currenProductIdInStock > 1;
		}

		private void UpdateProduct(Stock stock, string operation)
		{
			if (stock == null) {
					Error("Товар не найден");
					return;
			}
			if (!StockCheck(stock))
				return;
			if (Quantity.Value == null) {
				Error("Введите количество");
				return;
			}
			//списывать количество мы должны с загруженного объекта
			stock = Lines.Select(x => x.Stock).FirstOrDefault(x => x.Id == stock.Id) ?? stock;
			if (stock.Quantity < Quantity.Value) {
				Error("Нет требуемого количества");
				return;
			}
			Input.Value = null;
			Message(operation);
			var quantity = Quantity.Value.Value;
			var line = new CheckLine(stock, quantity, checkType);
			Lines.Add(line);
			CurrentLine.Value = line;
			Quantity.Value = null;
		}

		private void UpdateProduct(IQueryable<Stock> stocks, string operation)
		{
			//если элементов нет - выводим ошибку
			if (!stocks.Any()) {
				Error("Товар не найден");
				return;
			}
			//если элементов больше одного или для первого элемента есть выбор, предоставляем выбор пользователю
			if (stocks.Count() > 1 || HasStocksSimilarities(stocks.ToList())) {
				var result = SearchByTerm(stocks.First().Barcode);
				Coroutine.BeginExecute(result.GetEnumerator(), new ActionExecutionContext());
				return;
			}
			//если элемент один и нет возможного выбора, обрабатываем его
			UpdateProduct(stocks.First(), operation);
		}

		private bool StockCheck(Stock stock)
		{
			if (stock.RejectStatus == RejectStatus.Defective) {
				var cause = "";
				if (stock.RejectId.HasValue)
					cause = Env.Query(s => s.Get<Reject>(stock.RejectId)?.CauseRejects).Result;
				if (!Confirm($"Продажа забракованного товара. {cause}"))
					return false;
			}
			return true;
		}

		// Закрыть чек Enter
		public IEnumerable<IResult> Close()
		{
			if (Sum.Value.GetValueOrDefault() == 0) {
				Error("Чек не открыт");
				yield break;
			}
			var message = "Возврат по чеку";
			decimal charge = 0;
			decimal payment = 0;
			if (checkType == CheckType.SaleBuyer)
			{
				var checkout = new Checkout(Sum.Value.Value);
				yield return new DialogResult(checkout);
				Change.Value = checkout.Change.Value;
				charge = checkout.Change.Value.GetValueOrDefault();
				payment = checkout.Amount.Value.GetValueOrDefault();
				message = "Оплата наличными";
			}

			var waybillSettings = Settings.Value.Waybills.First(x => x.BelongsToAddress.Id == Address.Id);
			Env.Query(s => {
				var check = new Check(User, Address, Lines, checkType);
				check.Payment = payment;
				check.Charge = charge;
				using (var trx = s.BeginTransaction()) {
					s.Insert(check);
					Lines.Each(x => x.CheckId = check.Id);
					s.InsertEach(Lines);
					s.InsertEach(Lines.Select(x => new StockAction(ActionType.Sale, x.Stock, x.Quantity)));
					s.UpdateEach(Lines.Select(x => x.Stock).Distinct());
					trx.Commit();
				}

				if (!String.IsNullOrEmpty(Settings.Value.CheckPrinter))
					check.Print(Settings.Value.CheckPrinter, waybillSettings);
			}).Wait();
			Bus.SendMessage(nameof(Stock), "db");
			Bus.SendMessage(nameof(Check), "db");
			Message(message);
			Reset();
		}

		// Оплата/Возврат F4
		public void Trigger()
		{
			Reset();
			checkType = checkType == CheckType.SaleBuyer ? CheckType.CheckReturn : CheckType.SaleBuyer;
			Status.Value = StatusText();
		}

		private void Reset()
		{
			Lines.Clear();
			Status.Value = TXT_START_STATUS;
			Quantity.Value = null;
			PaymentType.Value = Models.Inventory.PaymentType.Cash;
		}

		// Смена типа оплаты F5
		public void SwitchPaymentType()
		{
			PaymentType.Value = PaymentType.Value == Models.Inventory.PaymentType.Cash
				? Models.Inventory.PaymentType.Cashless
				: Models.Inventory.PaymentType.Cash;
		}

		// Поиск товара по наименованию F6
		public IEnumerable<IResult> SearchByTerm(string term = "")
		{
			if (Quantity.Value == null) {
				Error("Введите количество");
				yield break;
			}
			if (String.IsNullOrEmpty(term))
				term = Input.Value;
			var model = new StockSearch(term);
			yield return new DialogResult(model, resizable: true);
			UpdateProduct(model.CurrentItem, "Поиск товара");
		}

		// Поиск товара по цене F7
		public IEnumerable<IResult> SearchByCost(decimal cost = 0)
		{
			if (Quantity.Value == null) {
				Error("Введите количество");
				yield break;
			}
			if (cost == 0)
				cost = Convert.ToDecimal(Input.Value);
			var model = new StockSearch(cost);
			yield return new DialogResult(model);
			UpdateProduct(model.CurrentItem, "Поиск товара");
		}

		// Вызов справки	F1
		public IEnumerable<IResult> Help()
		{
			yield return new DialogResult(new Help());
		}

		public IEnumerable<IResult> PossibleBarcodeScanned()
		{
			if (String.IsNullOrEmpty(Input.Value))
				yield break;
			var barcode = Input.Value;
			var stock = Env.Query(s => Stock.AvailableStocks(s, Address).Where(x => x.Barcode == barcode)).Result;
			if (!stock.Any())
				yield break;
			if (Quantity.Value == null)
				Quantity.Value = 1;
			UpdateProduct(stock, "Штрих код");
		}
		
		public class UnpackSettings
		{
			[Display(Name = "Количество")]
			public uint Quantity { get; set; }

			[Display(Name = "Кратность")]
			public int Multiplicity { get; set; }

			public UnpackSettings(decimal quantity)
			{
				Quantity = (uint)quantity;
			}
		}
		// Распаковка Ctrl+R
		public IEnumerable<IResult> Unpack()
		{
			var settings = new UnpackSettings(CurrentLine.Value.Quantity);
			yield return new DialogResult(new SimpleSettings(settings) { DisplayName = "Распаковка"});
			if (settings.Quantity <= 0 || settings.Multiplicity <= 0)
			{
				MessageBox.Show(
						"Суммы должны быть больше нуля",
						"АналитФАРМАЦИЯ: Внимание",
						MessageBoxButton.OK,
						MessageBoxImage.Error);
				yield break;
			}

			var srcStock = CurrentLine.Value.Stock;
			if (srcStock.Unpacked)
				yield break;

			srcStock.Quantity += CurrentLine.Value.Quantity;
			Lines.Remove(CurrentLine.Value);

			var doc = new UnpackingDoc(Address);
			var uline = new UnpackingLine(srcStock, settings.Multiplicity);
			doc.Lines.Add(uline);
			doc.UpdateStat();
			doc.Post();
			Session.Save(doc);
			Session.Flush();

			var line = new CheckLine(uline.DstStock, settings.Quantity, checkType);
			Lines.Add(line);
			CurrentLine.Value = line;
			Quantity.Value = null;
		}
	}
}
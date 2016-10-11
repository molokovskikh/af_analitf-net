using System;
using System.Collections.Generic;
using System.Linq;
using Common.Tools;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using NHibernate.Linq;
using ReactiveUI;
using Remotion.Linq.Collections;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class Frontend : BaseScreen2
	{
		public Frontend()
		{
			DisplayName = "Регистрация продаж";
			Lines = new ReactiveCollection<CheckLine>();
			Lines.Changed.Subscribe(_ => {
				if (Lines.Count == 0) {
					Status.Value = "Готов к работе";
					Discount.Value = null;
					Sum.Value = null;
				} else {
					Change.Value = null;
					Status.Value = "Открыт чек продажи";
					Sum.Value = Lines.Sum(x => x.RetailSum);
					Discount.Value = Lines.Sum(x => x.DiscontSum);
				}
			});
			Status.Value = "Готов к работе (F1 для справки)";
			checkType = CheckType.SaleBuyer;
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

		private CheckType checkType { get; set; }

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
			UpdateProduct(StockQuery().FirstOrDefault(x => x.ProductId == id), "Код товара");
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
			UpdateProduct(StockQuery().FirstOrDefault(x => x.Barcode == barcode), "Штрих код");
		}

		// Поиск по штрих-коду F3
		public void SearchByBarcode()
		{
			if (String.IsNullOrEmpty(Input.Value)) {
				Error("Ошибка ввода штрих-кода");
				return;
			}
			UpdateProduct(StockQuery().FirstOrDefault(x => x.Barcode == Input.Value), "Штрих код");
		}

		private IQueryable<Stock> StockQuery()
		{
			return Stock.AvailableStocks(StatelessSession, Address);
		}

		private void UpdateProduct(Stock stock, string operation)
		{
			if (stock == null) {
				Error("Товар не найден");
				return;
			}
			if (Quantity.Value == null) {
				Error("Введите количество");
				return;
			}
			//списывать количество мы должны с загруженного объекта
			var quantity = Quantity.Value.Value;
			stock = Lines.Select(x => x.Stock).FirstOrDefault(x => x.Id == stock.Id) ?? stock;
			if (checkType == CheckType.SaleBuyer && stock.Quantity < quantity) {
				Error("Нет требуемого количества");
				return;
			}
			Input.Value = null;
			Message(operation);

			var line = new CheckLine(stock, quantity, checkType);
			Lines.Add(line);
			CurrentLine.Value = line;
			Quantity.Value = null;
		}

		// Закрыть чек Enter
		public IEnumerable<IResult> Close()
		{
			if (Sum.Value.GetValueOrDefault() == 0) {
				Error("Чек не открыт");
				yield break;
			}
			var message = "Возврат по чеку";
			if (checkType == CheckType.SaleBuyer)
			{
				var checkout = new Checkout(Sum.Value.Value);
				yield return new DialogResult(checkout);
				Change.Value = checkout.Change.Value;
				message = "Оплата наличными";
			}

			using (var trx = StatelessSession.BeginTransaction()) {
				var check = new Check(Address, Lines, checkType);
				StatelessSession.Insert(check);
				Lines.Each(x => x.CheckId = check.Id);
				StatelessSession.InsertEach(Lines);
				StatelessSession.InsertEach(Lines.Select(x => new StockAction(ActionType.Sale, x.Stock, x.Quantity)));
				StatelessSession.UpdateEach(Lines.Select(x => x.Stock).Distinct());
				trx.Commit();
			}
			Bus.SendMessage(nameof(Stock), "db");
			Bus.SendMessage(nameof(Check), "db");
			Message(message);
			Reset();
		}

		// Оплата/Возврат F4
		public void Trigger()
		{
			if (checkType == CheckType.SaleBuyer)
			{
				checkType = CheckType.CheckReturn;
				Status.Value = "Открыт возврат по чеку";
			}
			else if (checkType == CheckType.CheckReturn)
			{
				checkType = CheckType.SaleBuyer;
				Status.Value = "Открыт чек продажи";
			}
			Reset();
		}

		private void Reset()
		{
			Lines.Clear();
			Status.Value = "Готов к работе";
			Quantity.Value = null;
		}

		// Поиск товара  F6
		public IEnumerable<IResult> SearchByTerm()
		{
			if (Quantity.Value == null) {
				Error("Введите количество");
				yield break;
			}
			var model = new StockSearch(Input.Value);
			yield return new DialogResult(model);
			UpdateProduct(model.CurrentItem, "Поиск товара");
		}

		// Вызов справки	F1
		public IEnumerable<IResult> Help()
		{
			yield return new DialogResult(new Help());
		}
	}
}
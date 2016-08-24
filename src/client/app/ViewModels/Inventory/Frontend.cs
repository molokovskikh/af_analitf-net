using System;
using System.Collections.Generic;
using System.Linq;
using Common.Tools;
using AnalitF.Net.Client.Helpers;
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
			Status.Value = "Готов к работе";
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

		public void UpdateQuantity()
		{
			var value = NullableConvert.ToUInt32(Input.Value);
			if (value == null) {
				Error("Ошибка ввода количества");
				return;
			}
			Input.Value = null;
			Message("Ввод количества");
			Quantity.Value = value.Value;
		}

		public void SearchByProductId()
		{
			var id = NullableConvert.ToUInt32(Input.Value);
			if (id == null) {
				Error("Ошибка ввода кода");
				return;
			}
			UpdateProduct(StockQuery().FirstOrDefault(x => x.ProductId == id), "Код товара");
		}

		private void Message(string text)
		{
			LastOperation.Value = text;
			HasError.Value = false;
		}

		private void Error(string message)
		{
			HasError.Value = true;
			Input.Value = null;
			LastOperation.Value = message;
		}

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
			return StatelessSession.Query<Stock>().Where(x => x.Address == Address);
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
			Input.Value = null;
			Message(operation);
			Lines.Add(new CheckLine(stock, Quantity.Value.Value));
			Quantity.Value = null;
		}

		public IEnumerable<IResult> Checkout()
		{
			if (Sum.Value.GetValueOrDefault() == 0) {
				Error("Чек не открыт");
			}
			var checkout = new Checkout(Sum.Value.Value);
			yield return new DialogResult(checkout);
			Change.Value = checkout.Change.Value;

			using (var trx = StatelessSession.BeginTransaction()) {
				var check = new Check(Address, Lines);
				StatelessSession.Insert(check);
				Lines.Each(x => x.CheckId = check.Id);
				StatelessSession.InsertEach(Lines);
				trx.Commit();
			}
			Lines.Clear();
			Message("Оплата наличными");
			Status.Value = "Готов к работе";
		}

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
	}
}
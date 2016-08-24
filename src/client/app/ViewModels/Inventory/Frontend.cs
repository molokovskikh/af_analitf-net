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
					Status.Value = "Открыт чек продажи";
					Sum.Value = Lines.Sum(x => x.RetailSum);
					Discount.Value = Lines.Sum(x => x.DiscontSum);
				}
			});
			Status.Value = "Готов к работе";
		}

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
			LastOperation.Value = "Ввод количества";
			Quantity.Value = value.Value;
		}

		public void SearchByProductId()
		{
			var id = NullableConvert.ToUInt32(Input.Value);
			if (id == null) {
				Error("Ошибка ввода кода");
				return;
			}
			UpdateProduct(StatelessSession.Query<Stock>().FirstOrDefault(x => x.ProductId == id), "Код товара");
		}

		private void Error(string message)
		{
			Input.Value = null;
			LastOperation.Value = message;
		}

		public void SearchByBarcode()
		{
			if (String.IsNullOrEmpty(Input.Value)) {
				Error("Ошибка ввода штрих-кода");
				return;
			}
			UpdateProduct(StatelessSession.Query<Stock>().FirstOrDefault(x => x.Barcode == Input.Value), "Штрих код");
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
			LastOperation.Value = operation;
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
			Input.Value = $"Сдача {checkout.Change}";
			var check = new Check(Address, Lines);
			Session.Save(check);
			Session.Flush();
			Session.Clear();
			Lines.Clear();
			Status.Value = "Готов к работе";
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using ReactiveUI;
using AnalitF.Net.Client.ViewModels.Parts;
using Common.Tools;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class Frontend2 : BaseScreen2, IEditor
	{
		public Frontend2()
		{
			DisplayName = "Регистрация продаж";
			InitFields();
			Lines = new ReactiveCollection<CheckLine>();
			Warning = new InlineEditWarning(Scheduler, Manager);
		}

		public NotifyValue<string> Input { get; set; }
		public NotifyValue<CheckLine> CurrentLine { get; set; }
		public ReactiveCollection<CheckLine> Lines { get; set; }
		public InlineEditWarning Warning { get; set; }

		// Закрыть чек Enter
		public IEnumerable<IResult> Close()
		{
			if (Lines.Count == 0) {
				Manager.Error("Чек не открыт");
				yield break;
			}
			var checkout = new Checkout(Lines.Sum(x => x.RetailSum));
			yield return new DialogResult(checkout);
			var charge = checkout.Change.Value.GetValueOrDefault();
			var payment = checkout.Amount.Value.GetValueOrDefault();

			var waybillSettings = Settings.Value.Waybills.First(x => x.BelongsToAddress.Id == Address.Id);
			Env.Query(s => {
				var check = new Check(User, Address, Lines, CheckType.SaleBuyer);
				check.Payment = payment;
				check.Charge = charge;
				using (var trx = s.BeginTransaction()) {
					s.Insert(check);
					Lines.Each(x => x.CheckId = check.Id);
					foreach (var line in check.Lines) {
						var stock = s.Get<Stock>(line.Stock.Id);
						stock.Quantity -= line.Quantity;
						s.Insert(line);
						s.Insert(new StockAction(ActionType.Sale, stock, line.Quantity));
						s.Update(stock);
					}
					trx.Commit();
				}

				if (!String.IsNullOrEmpty(Settings.Value.CheckPrinter))
					check.Print(Settings.Value.CheckPrinter, waybillSettings);
			}).Wait();
			Bus.SendMessage(nameof(Stock), "db");
			Bus.SendMessage(nameof(Check), "db");
			Reset();
		}

		private void Reset()
		{
			Lines.Clear();
		}

		public IEnumerable<IResult> Enter()
		{
			var inputValue = Input.Value;
			if (String.IsNullOrEmpty(inputValue))
				yield break;

			Input.Value = "";
			if (inputValue.Length > 8 && inputValue.All(x => char.IsDigit(x))) {
				foreach (var result in BarcodeScanned(inputValue)) {
					yield return result;
				}
				yield break;
			}

			var catalog = new CatalogChooser(inputValue, Address);
			yield return new DialogResult(catalog, resizable: true);
			var stockChooser = new StockChooser(catalog.CurrentItem.Value.CatalogId, Lines, Address);
			yield return new DialogResult(stockChooser, resizable: true);
			var ordered = stockChooser.Items.Value.Where(x => x.Ordered > 0).ToList();
			foreach(var item in ordered) {
				UpdateOrAddStock(item);
			}
		}

		private void UpdateOrAddStock(OrderedStock item)
		{
			var exists = Lines.FirstOrDefault(x => x.Stock.Id == item.Id);
			if (exists != null) {
				exists.Quantity = item.Ordered.Value;
			} else {
				Lines.Add(new CheckLine(item, item.Ordered.Value));
			}
		}

		private void AddStock(Stock item)
		{
			var exists = Lines.FirstOrDefault(x => x.Stock.Id == item.Id);
			if (exists != null) {
				exists.Quantity = 1;
			} else {
				Lines.Add(new CheckLine(item, 1));
			}
		}

		public class ExpSelector : Screen, ICancelable
		{
			public ExpSelector(DateTime[] exps)
			{
				WasCancelled = true;
				Exps = exps;
				CurrentExp = exps[0];
				DisplayName = "Укажите срок годности";
			}

			public string Name { get; set; }
			public DateTime[] Exps { get; set; }
			public DateTime CurrentExp { get; set; }

			public bool WasCancelled { get; set; }

			public void OK()
			{
				WasCancelled = false;
				TryClose();
			}
		}

		public IEnumerable<IResult> BarcodeScanned(string barcode)
		{
			var line = Lines.FirstOrDefault(x => x.Barcode == barcode && !x.Confirmed);
			if (line != null) {
				line.ConfirmedQuantity++;
				yield break;
			}
			var stocks = Env.Query(s => Stock.AvailableStocks(s, Address).Where(x => x.Barcode == barcode).ToArray()).Result;
			if (stocks.Length == 0) {
				Manager.Warning($"Товар с кодом {barcode} не найден");
				yield break;
			}
			if (stocks.Length == 1) {
				AddStock(stocks[0]);
				yield break;
			}

			var model = new ExpSelector(stocks.Select(x => x.Exp.GetValueOrDefault()).Distinct().OrderBy(x => x).ToArray());
			model.Name = $"Укажите срок годности - {stocks[0].Product}";
			yield return new DialogResult(model);
			var first = stocks.First(x => x.Exp == model.CurrentExp);
			AddStock(first);
		}

		public void Updated()
		{
			if (CurrentLine.Value.Quantity > CurrentLine.Value.Stock.Quantity) {
				Warning.Show(Common.Tools.Message.Warning($"Заказ превышает остаток на складе, товар будет заказан в количестве {CurrentLine.Value.Stock.Quantity}"));
				CurrentLine.Value.Quantity = CurrentLine.Value.Stock.Quantity;
			}
		}

		public void Committed()
		{
			if (CurrentLine.Value?.Quantity == 0)
				Lines.Remove(CurrentLine.Value);
		}
	}
}
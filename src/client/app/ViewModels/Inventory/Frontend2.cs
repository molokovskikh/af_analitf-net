﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using ReactiveUI;
using AnalitF.Net.Client.ViewModels.Parts;
using Common.Tools;
using NHibernate.Linq;
using AnalitF.Net.Client.ViewModels.Dialogs;


namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class Frontend2 : BaseScreen2, IEditor
	{
		public Frontend2()
		{
			DisplayName = "Регистрация продаж";
			InitFields();
			Status.Value = "Готов к работе(F1 для справки)";
			Lines = new ReactiveCollection<CheckLine>();
			CheckLinesForReturn = new List<CheckLine>();
			Warning = new InlineEditWarning(Scheduler, Manager);
			OnCloseDisposable.Add(SearchBehavior = new SearchBehavior(Env));
			SearchBehavior.ActiveSearchTerm.Where(x => !String.IsNullOrEmpty(x))
				.Subscribe(x => Coroutine.BeginExecute(Enter().GetEnumerator()));
			CurrentCatalog.Select(x => x?.Name?.Description != null)
				.Subscribe(CanShowDescription);
		}

		public SearchBehavior SearchBehavior { get; set; }
		public NotifyValue<CheckLine> CurrentLine { get; set; }
		public ReactiveCollection<CheckLine> Lines { get; set; }
		public InlineEditWarning Warning { get; set; }
		public NotifyValue<string> Status { get; set; }
		public CheckType? checkType { get; set; }
		private List<CheckLine> CheckLinesForReturn { get; set; }
		public NotifyValue<Catalog> CurrentCatalog { get; set; }
		public NotifyValue<decimal> CheckSum { get; set; }
		public NotifyValue<decimal> DiscontSum { get; set; }
		public NotifyValue<bool> CanShowDescription { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			Lines.Changed.Subscribe(_ => {
				if (Lines.Count > 0)
					Status.Value = (checkType == CheckType.SaleBuyer ? "Открыт чек продажи" : "Открыт возврат по чеку");
				else
				{
					Status.Value = "Готов к работе(F1 для справки)";
					CheckLinesForReturn.Clear();
					checkType = null;
				}
				CheckSum.Value = Lines.Sum(x => x.RetailSum);
				DiscontSum.Value = Lines.Sum(x => x.DiscontSum);
			});

			var lines = Shell.SessionContext.GetValueOrDefault(GetType().Name + "." + nameof(Lines)) as ReactiveCollection<CheckLine>;
			if (lines != null) {
				Lines.AddRange(lines);
			}
			Shell.SessionContext[GetType().Name + "." + nameof(Lines)] = null;

			CurrentLine
				.SelectMany(x => Env.RxQuery(s => {
					if (x == null)
						return null;
					var catalogId = x.CatalogId;
					return s.Query<Catalog>()
						.Fetch(c => c.Name)
						.ThenFetch(n => n.Mnn)
						.FirstOrDefault(c => c.Id == catalogId);
				}))
				.Subscribe(CurrentCatalog, CloseCancellation.Token);
		}

		protected override void OnDeactivate(bool close)
		{
			base.OnDeactivate(close);

			if (close) {
				if (Lines.Count > 0) {
					Shell.SessionContext[GetType().Name + "." + nameof(Lines)] = Lines;
				}
			}
		}

		public void Clear()
		{
			Lines.Clear();
		}

		// Закрыть чек Enter
		public IEnumerable<IResult> Close()
		{
			if (Lines.Count == 0) {
				Manager.Error("Чек не открыт");
				yield break;
			}
			var checkout = new Checkout(Lines.Sum(x => x.RetailSum));
			yield return new DialogResult(checkout);
			var check = new Check(User, Address, Lines, (CheckType)checkType);
			check.Charge = checkout.Change.Value.GetValueOrDefault();
			check.Payment = checkout.Amount.Value.GetValueOrDefault();
			check.PaymentByCard = checkout.CardAmount.Value.GetValueOrDefault();
			var waybillSettings = Settings.Value.Waybills.First(x => x.BelongsToAddress.Id == Address.Id);
			UnpackingDoc UnPackDoc = null;
			if (Lines.Where(x => x.SourceStock != null).Count() > 0)
			{
				UnPackDoc = new UnpackingDoc(Address, User);
				foreach (var line in check.Lines)
				{
					if (line.SourceStock != null)
					{
						var uline = new UnpackingLine(line.SourceStock, line.Stock);
						UnPackDoc.Lines.Add(uline);
					}
				}
				UnPackDoc.UpdateStat();
				UnPackDoc.Post();
			}
			Env.Query(s => {
				using (var trx = s.BeginTransaction()) {
					if (Lines.Where(x => x.SourceStock != null).Count() > 0)
					{
						s.Insert(UnPackDoc);
						UnPackDoc.Lines.Each(x => x.UnpackingDocId = UnPackDoc.Id);
						foreach (var uline in UnPackDoc.Lines)
						{
							s.Insert("AnalitF.Net.Client.Models.Inventory.Stock", uline.DstStock);
							s.Update("AnalitF.Net.Client.Models.Inventory.Stock", uline.SrcStock);
							s.Insert(uline);
							UnPackDoc.PostStockActions();
							s.Insert(uline.SrcStockAction);
							s.Insert(uline.DstStockAction);
						}
					}
					s.Insert(check);
					Lines.Each(x => x.CheckId = check.Id);
					Lines.Each(x => x.Doc = check);
					foreach (var line in check.Lines) {
						if (line.Stock != null)
						{
							var stock = s.Get<Stock>(line.Stock.Id);
							s.Insert(line.UpdateStock(stock, (CheckType)checkType));
							s.Update(stock);
						}
						s.Insert(line);

					}
					trx.Commit();
				}

				if (!String.IsNullOrEmpty(Settings.Value.CheckPrinter))
					check.Print(Settings.Value.CheckPrinter, waybillSettings);
			}).Wait();
			Bus.SendMessage(nameof(Stock), "db");
			Bus.SendMessage(nameof(Check), "db");
			Bus.SendMessage(nameof(UnpackingDoc), "db");
			Reset();
		}

		private void Reset()
		{
			Lines.Clear();
		}

		public IEnumerable<IResult> Enter()
		{
			var inputValue = SearchBehavior.ActiveSearchTerm.Value;
			if (String.IsNullOrEmpty(inputValue))
				yield break;

			if (checkType == CheckType.CheckReturn)
			{
				Warning.Show(Common.Tools.Message.Warning($"При открытом документе возврат, подбор не возможен"));
				yield break;
			}
			SearchBehavior.ActiveSearchTerm.Value = null;
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
			if (checkType == null)
				checkType = CheckType.SaleBuyer;
			var exists = Lines.FirstOrDefault(x => x.Stock.Id == item.Id);
			if (exists != null) {
				exists.Quantity = item.Ordered.Value;
				CurrentLine.Value = exists;
			} else {
				var checkLine = new CheckLine(item, item.Ordered.Value);
				Lines.Add(checkLine);
				CurrentLine.Value = checkLine;
			}
		}

		private void AddStock(Stock item)
		{
			if (checkType == null)
				checkType = CheckType.SaleBuyer;
			var exists = Lines.FirstOrDefault(x => x.Stock.Id == item.Id);
			if (exists != null) {
				exists.Quantity = 1;
				CurrentLine.Value = exists;
			} else {
				var line = new CheckLine(item, 1);
				Lines.Add(line);
				CurrentLine.Value = line;
			}
		}

		private void AddBarcodeProduct(BarcodeProducts item, uint quantity, decimal retailCost)
		{
			if (checkType == null)
				checkType = CheckType.SaleBuyer;
			var exists = Lines.FirstOrDefault(x => x.BarcodeProduct.Id == item.Id && x.RetailCost == retailCost);
			if (exists != null)
			{
				exists.Quantity += quantity;
			}
			else
			{
				var line = new CheckLine(item, quantity, retailCost);
				Lines.Add(line);
				CurrentLine.Value = line;
			}
		}

		public IEnumerable<IResult> BarcodeScanned(string barcode)
		{
			if (checkType == CheckType.CheckReturn)
			{
				Warning.Show(Common.Tools.Message.Warning($"При открытом документе возврат, сканирование не возможено"));
				yield break;
			}
			var line = Lines.FirstOrDefault(x => x.Barcode == barcode && !x.Confirmed);
			if (line != null) {
				line.ConfirmedQuantity++;
				yield break;
			}
			var stocks = Env.Query(s => Stock.AvailableStocks(s, Address).Where(x => x.Barcode == barcode).ToArray()).Result;
			if (stocks.Length == 0) {
				if (Settings.Value.FreeSale)
				{
					BarcodeProducts BarcodeProduct = Session.Query<BarcodeProducts>()
						.Where(x => x.Barcode == barcode).FirstOrDefault();
					if (BarcodeProduct != null)
					{
						var inputQuantity = new InputQuantityRetailCost(BarcodeProduct);
						yield return new DialogResult(inputQuantity, resizable: false);
						if (!inputQuantity.WasCancelled)
						{
							AddBarcodeProduct(inputQuantity.BarcodeProduct.Value,
								(uint)inputQuantity.Quantity.Value, (decimal)inputQuantity.RetailCost.Value);
							yield break;
						}
					}
				}
				else
				{
					Manager.Warning($"Товар с кодом {barcode} не найден");
					yield break;
				}
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
			if (checkType == CheckType.SaleBuyer)
			{
				var stockQuantity = CurrentLine.Value.Stock.Quantity;
				if (CurrentLine.Value.Quantity > stockQuantity)
				{
					Warning.Show(Common.Tools.Message.Warning(
						$"Заказ превышает остаток на складе, товар будет заказан в количестве {stockQuantity}"));
					CurrentLine.Value.Quantity = stockQuantity;
				}
			}
			if (checkType == CheckType.CheckReturn)
			{
				var stockQuantity = CheckLinesForReturn
					.Where(x => x.Stock.Id == CurrentLine.Value.Stock.Id)
					.First().Quantity;
				if (CurrentLine.Value.Quantity > stockQuantity)
				{
					Warning.Show(Common.Tools.Message.Warning(
						$"Количество возврата превышает проданное, товар будет возвращен в количестве {stockQuantity}"));
					CurrentLine.Value.Quantity = stockQuantity;
				}
			}
			CheckSum.Value = Lines.Sum(x => x.RetailSum);
			DiscontSum.Value = Lines.Sum(x => x.DiscontSum);
		}

		public void Committed()
		{
			if (CurrentLine.Value?.Quantity == 0)
				Lines.Remove(CurrentLine.Value);
		}

		// Распаковка Ctrl+U
		public IEnumerable<IResult> Unpack()
		{
			if (checkType == CheckType.CheckReturn)
			{
				Warning.Show(Common.Tools.Message.Warning($"Распаковка при возврате не возможена"));
				yield break;
			}
			var srcStock = CurrentLine.Value.Stock;
			if (srcStock == null)
			{
				Warning.Show(Common.Tools.Message.Warning($"Не определен товар для распаковки"));
				yield break;
			}
			if (srcStock.Unpacked)
				yield break;

			var inputQuantity = new InputQuantity((OrderedStock)srcStock);
			yield return new DialogResult(inputQuantity, resizable: false);
			if (!inputQuantity.WasCancelled)
			{
				Lines.Remove(CurrentLine.Value);
				checkType = CheckType.SaleBuyer;
				var line = new CheckLine(inputQuantity.DstStock, inputQuantity.SrcStock, inputQuantity.DstStock.Value.Ordered.Value);
				Lines.Add(line);
				CurrentLine.Value = line;
			}
		}

		public IEnumerable<IResult> ReturnCheck()
		{
			if (checkType == CheckType.SaleBuyer)
			{
				Warning.Show(Common.Tools.Message.Warning($"При открытом документе продажи, возврат не возможен"));
				yield break;
			}
			var Checks = new Checks(true);
			yield return new DialogResult(Checks, resizable: true);
			if (!Checks.DialogCancelled && Checks.CurrentItem != null)
			{
				if (((Check)Checks.CurrentItem).CheckType == CheckType.CheckReturn)
				{
					Warning.Show(Common.Tools.Message.Warning($"По документу возврат, возврат не возможен"));
					yield break;
				}
				checkType = CheckType.CheckReturn;
				CheckLinesForReturn = Session.Query<CheckLine>()
					.Where(x => x.CheckId == ((Check)Checks.CurrentItem).Id)
					.Fetch(x => x.Stock)
					.ToList();

				foreach (var line in CheckLinesForReturn)
				{
					var checkLine = new CheckLine(line.Stock, (uint)line.Quantity, CheckType.CheckReturn);
					Lines.Add(checkLine);
					CurrentLine.Value = checkLine;
				}
			}
		}

		// Вызов справки	F1
		public IEnumerable<IResult> Help()
		{
			yield return new DialogResult(new Help());
		}

		public void ShowDescription()
		{
			if (!CanShowDescription)
				return;
			Manager.ShowDialog(new DocModel<ProductDescription>(CurrentCatalog.Value.Name.Description.Id));
		}
	}
}
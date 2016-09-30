﻿using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using Common.Tools;
using NPOI.SS.UserModel;
using System.Collections.ObjectModel;
using NPOI.HSSF.UserModel;
using System;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.ViewModels.Parts;
using Common.NHibernate;
using NHibernate;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class Stocks : BaseScreen2
	{
		private string Name;

		public Stocks()
		{
			DisplayName = "Товарные запасы";
			AddressSelector = new AddressSelector(this);
			Items.PropertyChanged += Items_PropertyChanged;
			ItemsTotal = new ObservableCollection<StockTotal>();
			ItemsTotal.Add(new StockTotal { Total = "Итого", });

			Name = User?.FullName ?? "";
			StatusFilter.Value = DescriptionHelper.GetDescriptions(typeof(StockStatus))
				.Select(x => new Selectable<StockStatus>((StockStatus)x.Value, x.Name))
				.ToList();
			QuickSearch = new QuickSearch<Stock>(UiScheduler,
				t => Items?.Value.FirstOrDefault(p => p.Product.IndexOf(t, StringComparison.CurrentCultureIgnoreCase) >= 0),
				CurrentItem);
			TrackDb(typeof(Stock));
		}

		public QuickSearch<Stock> QuickSearch { get; set; }
		public NotifyValue<List<Stock>> Items { get; set; }
		public NotifyValue<Stock> CurrentItem { get; set; }
		public ObservableCollection<StockTotal> ItemsTotal { get; set; }
		public AddressSelector AddressSelector { get; set; }
		public NotifyValue<IList<Selectable<StockStatus>>> StatusFilter { get; set; }

		private void Items_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (Items.Value == null) return;

			ItemsTotal[0].TotalCount = Items.Value.Sum(c => c.Quantity);
			ItemsTotal[0].ReservedQuantity = Items.Value.Sum(c => c.ReservedQuantity);
			ItemsTotal[0].TotalSum = Items.Value.Sum(c => c.SupplySumWithoutNds);
			ItemsTotal[0].TotalSumWithNds = Items.Value.Sum(c => c.SupplySum);
			ItemsTotal[0].TotalRetailSum = Items.Value.Sum(c => c.RetailSum).GetValueOrDefault();
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();
			AddressSelector.Init();

			Bus.Listen<string>("reload").Cast<object>()
				.Merge(DbReloadToken)
				.Merge(StatusFilter.FilterChanged())
				.Merge(AddressSelector.FilterChanged.Cast<object>())
				.SelectMany(_ => RxQuery(x => {
					var query = x.Query<Stock>().Where(y => y.Quantity != 0 || y.ReservedQuantity != 0);
					if (StatusFilter.IsFiltred()) {
						var values = StatusFilter.GetValues();
						query = query.Where(y => values.Contains(y.Status));
					}
					var addresses = AddressSelector.GetActiveFilter().Select(y => y.Id);
					query = query.Where(y => addresses.Contains(y.Address.Id));
					return query.OrderBy(y => y.Product).ToList();
				}))
				.Subscribe(Items, CloseCancellation.Token);
		}

		protected override void OnDeactivate(bool close)
		{
			base.OnDeactivate(close);
			AddressSelector.OnDeactivate();
		}

		public IEnumerable<IResult> EnterItems()
		{
			var stock = CurrentItem.Value;
			if (stock != null)
				yield return new DialogResult(new EditStock(stock.Id));

			Session.Refresh(stock);
			Update();
		}

		public IResult ExportExcel()
		{
			var columns = new[] {"Штрих-код",
				"Название товара",
				"Фирма-производитель",
				"Статус",
				"Заказ на приемку",
				"Кол-во",
				"Цена закупки",
				"Цена розничная",
				"Сумма закупки",
				"Сумма закупки с НДС",
				"Сумма розничная",
				"Кол-во поставки"};

			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("Экспорт");
			var items = Items.Value;
			var row = 0;

			ExcelExporter.WriteRow(sheet, columns, row++);

			var rows = Items.Value.Select((o, i) => new object[] {
				o.Barcode,
				o.Product,
				o.Producer,
				o.Status,
				o.ReceivingOrderId,
				o.Quantity,
				o.SupplierCost,
				o.RetailCost,
				o.SupplySumWithoutNds,
				o.SupplySum,
				o.RetailSum,
				o.SupplyQuantity
			});

			row = ExcelExporter.WriteRows(sheet, rows, row);

			if (items.Count > 0)
				WriteStatRow(sheet, row, items, "Итого");

			return ExcelExporter.Export(book);
		}

		private static int WriteStatRow(ISheet sheet, int row, IEnumerable<Stock> items, string label)
		{
			var statRow = sheet.CreateRow(row++);
			ExcelExporter.SetCellValue(statRow, 4, label);
			ExcelExporter.SetCellValue(statRow, 5, items.Sum(x => x.Quantity));
			var total = items.Sum(x => x.SupplySumWithoutNds);
			ExcelExporter.SetCellValue(statRow, 8, total);
			var totalWithNds = items.Sum(x => x.SupplySum);
			ExcelExporter.SetCellValue(statRow, 9, totalWithNds);
			var retailTotal = items.Sum(x => x.RetailSum);
			ExcelExporter.SetCellValue(statRow, 10, retailTotal);
			return row;
		}

		public IEnumerable<IResult> PrintStock()
		{
			return Preview("Товарные запасы", new StockDocument(Items.Value.ToArray()));
		}

		private IEnumerable<IResult> Preview(string name, BaseDocument doc)
		{
			var docSettings = doc.Settings;
			if (docSettings != null)
			{
				yield return new DialogResult(new SimpleSettings(docSettings));
			}
			yield return new DialogResult(new PrintPreviewViewModel(new PrintResult(name, doc)), fullScreen: true);
		}

		public IResult PrintStockPriceTags()
		{
			return new DialogResult(new PrintPreviewViewModel
			{
				DisplayName = "Ценники",
				Document = new StockPriceTagDocument(Items.Value.Cast<BaseStock>().ToList(), Name).Build()
			}, fullScreen: true);
		}

		public IResult PrintStockRackingMaps()
		{
			var receivingOrders = Session.Query<ReceivingOrder>().ToList();

			return new DialogResult(new PrintPreviewViewModel
			{
				DisplayName = "Постелажная карта",
				Document = new StockRackingMapDocument(receivingOrders, Items.Value.ToList()).Build()
			}, fullScreen: true);
		}

		public IEnumerable<IResult> PrintStockLimitMonth()
		{
			var title = "Товары со сроком годности менее 1 месяца";
			var stocks = Items.Value.Where(s => s.Exp < DateTime.Today.AddMonths(1)).ToArray();
			return Preview(title, new StockLimitMonthDocument(stocks, title, Name));
		}

		public IEnumerable<IResult> PrintStockLimit()
		{
			var stocks = Items.Value.Where(s => !String.IsNullOrEmpty(s.Period)).ToList();
			yield return new DialogResult(new SelectStockPeriod(stocks, Name));
		}

		public void CheckDefectSeries()
		{
			Shell.Navigate(new CheckDefectSeries());
		}

		public void ReceivingOrders()
		{
			Shell.Navigate(new ReceivingOrders());
		}

		public void Checks()
		{
			Shell.Navigate(new Checks());
		}

		public void InventoryDocs()
		{
			Shell.Navigate(new InventoryDocs());
		}

		public void WriteoffDocs()
		{
			Shell.Navigate(new WriteoffDocs());
		}

		public void ReturnToSuppliers()
		{
			Shell.Navigate(new ReturnToSuppliers());
		}
	}
}
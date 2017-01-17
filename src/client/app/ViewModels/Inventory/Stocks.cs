using System.Collections.Generic;
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
using System.ComponentModel;
using System.Printing;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.ViewModels.Parts;
using Common.NHibernate;
using Diadoc.Api.Proto.Documents;
using NHibernate;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class Stocks : BaseScreen2, IPrintableStock
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

			PrintStockMenuItems = new ObservableCollection<MenuItem>();
			IsView = true;
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
					return query.Fetch(y => y.Address).OrderBy(y => y.Product).ToList();
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
			var receivingOrders = Session.Query<Waybill>().ToList();
			return new DialogResult(new PrintPreviewViewModel
			{
				DisplayName = "Постеллажная карта",
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

		public void GoodsMovement()
		{
			Shell.Navigate(new GoodsMovement());
		}

		public void ReceivingOrders()
		{
			Shell.Navigate(new WaybillsViewModel());
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

		public void ReassessmentDocs()
		{
			Shell.Navigate(new ReassessmentDocs());
		}

		public void DisplacementDocs()
		{
			Shell.Navigate(new DisplacementDocs());
		}

		public void UnpackingDocs()
		{
			Shell.Navigate(new UnpackingDocs());
		}

		public void SetMenuItems()
		{
			var item = new MenuItem {Header = "Ценники"};
			PrintStockMenuItems.Add(item);

			item = new MenuItem {Header = "Товарные запасы"};
			PrintStockMenuItems.Add(item);

			item = new MenuItem {Header = "Товары со сроком годности"};
			PrintStockMenuItems.Add(item);

			item = new MenuItem {Header = "Товары со сроком годности менее 1 месяца"};
			PrintStockMenuItems.Add(item);

			item = new MenuItem {Header = "Постеллажная карта"};
			PrintStockMenuItems.Add(item);
		}

		PrintResult IPrintableStock.PrintStock()
		{
			var docs = new List<BaseDocument>();
			if (!IsView) {
				foreach (var item in PrintStockMenuItems.Where(i => i.IsChecked)) {
					if ((string)item.Header == "Товарные запасы")
						docs.Add(new StockDocument(Items.Value.ToArray()));
					if ((string)item.Header == "Ценники")
						PrintFixedDoc(new StockPriceTagDocument(Items.Value.Cast<BaseStock>().ToList(), Name).Build().DocumentPaginator, "Ценники");
					if ((string)item.Header == "Товары со сроком годности") {
						var stocks = Items.Value.Where(s => !String.IsNullOrEmpty(s.Period)).ToList();
						var per = new DialogResult(new SelectStockPeriod(stocks, Name));
						per.Execute(null);
					}
					if ((string)item.Header == "Товары со сроком годности менее 1 месяца")
						docs.Add(new StockLimitMonthDocument(Items.Value.Where(s => s.Exp < DateTime.Today.AddMonths(1)).ToArray(),
							"Товары со сроком годности менее 1 месяца", Name));
					if ((string)item.Header == "Постеллажная карта") {
						var receivingOrders = Session.Query<Waybill>().ToList();
						PrintFixedDoc(new StockRackingMapDocument(receivingOrders, Items.Value.ToList()).Build().DocumentPaginator, "Постеллажная карта");
					}
				}
				return new PrintResult(DisplayName, docs, PrinterName);
			}

			if(string.IsNullOrEmpty(LastOperation) || LastOperation == "Товарные запасы")
				Coroutine.BeginExecute(PrintStock().GetEnumerator());
			if(LastOperation == "Ценники")
				PrintStockPriceTags().Execute(null);
			if(LastOperation == "Товары со сроком годности")
				Coroutine.BeginExecute(PrintStockLimit().GetEnumerator());
			if(LastOperation == "Товары со сроком годности менее 1 месяца")
				Coroutine.BeginExecute(PrintStockLimitMonth().GetEnumerator());
			if(LastOperation == "Постеллажная карта")
				PrintStockRackingMaps().Execute(null);
			return null;
		}

		private void PrintFixedDoc(DocumentPaginator doc, string name)
		{
			var dialog = new PrintDialog();
			if (!string.IsNullOrEmpty(PrinterName)) {
				dialog.PrintQueue = new PrintQueue(new PrintServer(), PrinterName);
				dialog.PrintDocument(doc, name);
			}
			else if (dialog.ShowDialog() == true)
				dialog.PrintDocument(doc, name);
		}
		public ObservableCollection<MenuItem> PrintStockMenuItems { get; set; }
		public string LastOperation { get; set; }
		public string PrinterName { get; set; }
		public bool IsView { get; set; }

		public bool CanPrintStock
		{
			get { return true; }
		}

	}
}
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
	public class Stocks : BaseScreen2, IPrintable
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

			PrintMenuItems = new ObservableCollection<MenuItem>();
			IsView = true;
			CurrentItem.Select(x => x?.WaybillId != null).Subscribe(CanOpenWaybill);
		}

		public QuickSearch<Stock> QuickSearch { get; set; }
		public NotifyValue<List<Stock>> Items { get; set; }
		public NotifyValue<Stock> CurrentItem { get; set; }
		public ObservableCollection<StockTotal> ItemsTotal { get; set; }
		public AddressSelector AddressSelector { get; set; }
		public NotifyValue<IList<Selectable<StockStatus>>> StatusFilter { get; set; }
		public NotifyValue<bool> CanOpenWaybill { get; set; }

		private void Items_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (Items.Value == null) return;

			ItemsTotal[0].TotalCount = Items.Value.Sum(c => c.Quantity);
			ItemsTotal[0].ReservedQuantity = Items.Value.Sum(c => c.ReservedQuantity);
			ItemsTotal[0].TotalSum = Items.Value.Sum(c => c.SupplySumWithoutNds);
			ItemsTotal[0].TotalSumWithNds = Items.Value.Sum(c => c.SupplySum);
			ItemsTotal[0].TotalRetailSum = Items.Value.Sum(c => c.RetailSum).GetValueOrDefault();

			//В некоторых случаях у грида в адресной колонке ActualWidth меньше чем на самом деле,
			//из-за чего нижний грид смещается, т.к. ориентируется на AddressColumn.ActualWidth
			//Исправляем синхронизацией ActualWidth с реальной шириной
			var view = (Views.Inventory.Stocks)GetView();
			if (view != null) view.AddressColumn.Width = view.AddressColumn.ActualWidth;
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();
			AddressSelector.Init();
			AddressSelector.Description = "Все накладные";

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

		public IEnumerable<IResult> Print()
		{
			return Preview("Товарные запасы", new StockDocument(Items.Value.ToArray()));
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

		public void Tags()
		{
			var tags = Items.Value.Select(x => x.GeTagPrintable(User?.FullName)).ToList();
			Shell.Navigate(new Tags(Address, tags));
		}

		public void CheckDefectSeries()
		{
			Shell.Navigate(new CheckDefectSeries());
		}

		public void ShelfLife()
		{
			Shell.Navigate(new ShelfLife());
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

		public void OpenWaybill()
		{
			Shell.Navigate(new WaybillDetails(CurrentItem.Value.WaybillId.Value));
		}

		public void SetMenuItems()
		{
			var item = new MenuItem {Header = "Ярлыки"};
			PrintMenuItems.Add(item);

			item = new MenuItem {Header = "Товарные запасы"};
			PrintMenuItems.Add(item);

			item = new MenuItem {Header = "Товары со сроком годности"};
			PrintMenuItems.Add(item);

			item = new MenuItem {Header = "Товары со сроком годности менее 1 месяца"};
			PrintMenuItems.Add(item);
		}

		PrintResult IPrintable.Print()
		{
			var docs = new List<BaseDocument>();

			if (!IsView) {
				foreach (var item in PrintMenuItems.Where(i => i.IsChecked)) {
					if ((string)item.Header == "Товарные запасы")
						docs.Add(new StockDocument(Items.Value.ToArray()));
					if ((string) item.Header == "Ярлыки")
						Tags();
					if ((string)item.Header == "Товары со сроком годности") {
						var stocks = Items.Value.Where(s => !String.IsNullOrEmpty(s.Period)).ToList();
						var per = new DialogResult(new SelectStockPeriod(stocks, Name));
						per.Execute(null);
					}
					if ((string)item.Header == "Товары со сроком годности менее 1 месяца")
						docs.Add(new StockLimitMonthDocument(Items.Value.Where(s => s.Exp < DateTime.Today.AddMonths(1)).ToArray(),
							"Товары со сроком годности менее 1 месяца", Name));
				}
				return new PrintResult(DisplayName, docs, PrinterName);
			}

			if(string.IsNullOrEmpty(LastOperation) || LastOperation == "Товарные запасы")
				Coroutine.BeginExecute(Print().GetEnumerator());
			if(LastOperation == "Ярлыки")
				Tags();
			if(LastOperation == "Товары со сроком годности")
				Coroutine.BeginExecute(PrintStockLimit().GetEnumerator());
			if(LastOperation == "Товары со сроком годности менее 1 месяца")
				Coroutine.BeginExecute(PrintStockLimitMonth().GetEnumerator());
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
		public ObservableCollection<MenuItem> PrintMenuItems { get; set; }
		public string LastOperation { get; set; }
		public string PrinterName { get; set; }
		public bool IsView { get; set; }

		public bool CanPrint
		{
			get { return true; }
		}

		protected override void OnViewLoaded(object view)
		{
			base.OnViewLoaded(view);

			SetColumnsForGrid();
		}

		/// <summary>
		/// Обновление представления столбцов пользователем.
		/// Синхронизируем гриды
		/// </summary>
		public override void UpdateColumns()
		{
			SetColumnsForGrid();
		}

		/// <summary>
		/// Синхронизируем нижний грид с верхним
		/// </summary>
		private void SetColumnsForGrid()
		{
			var view = (Views.Inventory.Stocks)GetView();
			for (int i = 0; i < view.Items.Columns.Count; i++)
			{
				view.DgItemsTotal.Columns[i].Visibility = view.Items.Columns[i].Visibility;
				view.DgItemsTotal.Columns[i].DisplayIndex = view.Items.Columns[i].DisplayIndex;
			}
		}

	}
}
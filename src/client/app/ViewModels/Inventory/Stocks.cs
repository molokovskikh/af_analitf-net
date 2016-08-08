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
using Common.NHibernate;
using NHibernate;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class Stocks : BaseScreen2
	{
		public NotifyValue<List<Stock>> Items { get; set; }
		public NotifyValue<Stock> CurrentItem { get; set; }
		public ObservableCollection<StockTotal> ItemsTotal { get; set; }

		private string Name;

		public Stocks()
		{
			Items.PropertyChanged += Items_PropertyChanged;
			CurrentItem = new NotifyValue<Stock>();
			ItemsTotal = new ObservableCollection<StockTotal>();
			ItemsTotal.Add(new StockTotal { Total = "Итого", TotalCount = 0.0m, TotalSum = 0.0m, TotalSumWithNds = 0.0m, TotalRetailSum = 0.0m });

			Name = User?.FullName ?? "";
		}

		private void Items_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (Items.Value == null || ItemsTotal.Count != 1) return;

			ItemsTotal.First().TotalCount = Items.Value.Sum(c => c.Count);
			ItemsTotal.First().TotalSum = Items.Value.Sum(c => c.Sum);
			ItemsTotal.First().TotalSumWithNds = Items.Value.Sum(c => c.SumWithNds);
			ItemsTotal.First().TotalRetailSum = Items.Value.Sum(c => c.RetailSum);
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			RxQuery(x => x.Query<Stock>().OrderBy(y => y.Product).ToList())
				.Subscribe(Items);
		}

		public IEnumerable<IResult> EnterItems()
		{
			var stock = CurrentItem.Value;
			if (stock != null)
				yield return new DialogResult(new EditStock(stock.Id));

			Session.Refresh(stock);
			Update();
		}

		public override void Update()
		{
			var query = StatelessSession.Query<Stock>();
			Items.Value = query.ToList();
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
				o.Count,
				o.Cost,
				o.RetailCost,
				o.Sum,
				o.SumWithNds,
				o.RetailSum,
				o.CountDelivery
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
			ExcelExporter.SetCellValue(statRow, 5, items.Sum(x => x.Count));
			var total = items.Sum(x => x.Sum);
			ExcelExporter.SetCellValue(statRow, 8, total);
			var totalWithNds = items.Sum(x => x.SumWithNds);
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
				Document = new StockPriceTagDocument(Items.Value.ToList(), Name).Build()
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
			string title = "Товары со сроком годности менее 1 месяца";
			var stocks = Items.Value.Where(s => Convert.ToDateTime(s.Period) < DateTime.Today.AddMonths(1) && !String.IsNullOrEmpty(s.Period)).ToArray();
			return Preview(title, new StockLimitMonthDocument(stocks, title, Name));
		}

		public IEnumerable<IResult> PrintStockLimit()
		{
			var stocks = Items.Value.Where(s => !String.IsNullOrEmpty(s.Period)).ToList();
			yield return new DialogResult(new SelectStockPeriod(stocks, Name));
		}

		public static bool StockWaybill(ISession session, Waybill waybill)
		{
			var order = new ReceivingOrder(waybill);
			if (order.Lines.Count == 0)
				return false;
			if (order.Lines.Count > 0)
				session.Save(order);
			session.SaveEach(order.ToStocks());
			return true;
		}
	}
}
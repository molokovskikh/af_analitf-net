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
using System.Collections.ObjectModel;
using NPOI.HSSF.UserModel;
using System;
using System.Windows;
using System.Windows.Controls;
using NHibernate;
using NPOI.SS.UserModel;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class ShelfLife : BaseScreen2, IPrintableStock
	{
		public NotifyValue<List<Stock>> Items { get; set; }
		public NotifyValue<Stock> CurrentItem { get; set; }
		public NotifyValue<bool> IsNotOverdue { get; set; }
		public NotifyValue<bool> IsOverdue { get; set; }
		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }

		public ShelfLife()
		{
			DisplayName = "Отчет по срокам годности";
			Begin = new NotifyValue<DateTime>(DateTime.Today.AddMonths(-3));
			End = new NotifyValue<DateTime>(DateTime.Today.AddMonths(3));
			IsNotOverdue = new NotifyValue<bool>(true);
			IsOverdue = new NotifyValue<bool>(true);

			PrintStockMenuItems = new ObservableCollection<MenuItem>();
			IsView = true;
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			//var ff = Session.Query<Stock>().Single(x => x.Period == "09.01.2017");
			//ff.Period = "09.12.2016";
			//Session.Flush();

			DbReloadToken
				.Merge(Begin.Changed())
				.Merge(End.Changed())
				.Merge(IsNotOverdue.Changed())
				.Merge(IsOverdue.Changed())
				.SelectMany(_ => RxQuery(LoadItems))
				.Subscribe(Items);
		}

		public List<Stock> LoadItems(IStatelessSession session)
		{
			var items = Stock.AvailableStocks(session).OrderBy(y => y.Product)
				.ToList()
				.Where(x => x.ParsedPeriod >= Begin.Value && x.ParsedPeriod < End.Value.AddDays(1))
				.OrderBy(x => x.ParsedPeriod)
				.ThenBy(x => x.Product)
				.ToList();

			if (!IsOverdue)
				items = items.Where(x => !x.IsOverdue).ToList();
			if (!IsNotOverdue)
				items = items.Where(x => x.IsOverdue).ToList();

			return items;
		}

		public IResult ExportExcel()
		{
			var visibilityDic = new Dictionary<string, bool>();
			var grid = GetControls(GetView()).SingleOrDefault(x => x.Name == "Items");
			if (grid != null)
				visibilityDic = grid.Columns.ToDictionary(x => x.SortMemberPath, x => x.Visibility == Visibility.Visible);

			var colObj = new[]
			{
				Tuple.Create("Period", (object)"Срок годности"),
				Tuple.Create("Product", (object)"Торговое наименование"),
				Tuple.Create("SerialNumber", (object)"Серия"),
				Tuple.Create("Producer", (object)"Производитель"),
				Tuple.Create("Quantity", (object)"Кол-во"),
				Tuple.Create("WaybillNumber", (object)"Номер накладной"),
				Tuple.Create("SupplierFullName", (object)"Поставщик"),
			};
			var columns = Remap(colObj, visibilityDic);

			Func<Stock, object[]> toRow = x =>
			{
				var obj = new[]
				{
					Tuple.Create("Period", (object)x.Period),
					Tuple.Create("Product", (object)x.Product),
					Tuple.Create("SerialNumber", (object)x.SerialNumber),
					Tuple.Create("Producer", (object)x.Producer),
					Tuple.Create("Quantity", (object)x.Quantity),
					Tuple.Create("WaybillNumber", (object)x.WaybillNumber),
					Tuple.Create("SupplierFullName", (object)x.SupplierFullName),
				};
				return Remap(obj, visibilityDic);
			};

			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("Экспорт");
			var items = Items.Value;
			var groups = items.GroupBy(x => x.PeriodMonth);
			var row = 0;
			ExcelExporter.WriteRow(sheet, columns, row++);
			foreach (var group in groups) {
				row = WriteStatRow(sheet, row, @group, group.Key, visibilityDic);
				row = ExcelExporter.WriteRows(sheet, group.OrderBy(x => x.Product).Select(toRow), row);
			}

			return ExcelExporter.Export(book);
		}

		private static int WriteStatRow(ISheet sheet, int row, IEnumerable<Stock> items, string label, Dictionary<string, bool> dic)
		{
			var obj = new[] {
				Tuple.Create("Period", (object)label),
				Tuple.Create("Product", (object)null),
				Tuple.Create("SerialNumber", (object)null),
				Tuple.Create("Producer", (object)null),
				Tuple.Create("Quantity", (object)null),
				Tuple.Create("WaybillNumber", (object)null),
				Tuple.Create("SupplierFullName", (object)null),
			};
			var result = Remap(obj, dic);
			ExcelExporter.WriteRow(sheet, result, row++);
			return row;
		}

		private static object[] Remap(Tuple<string, object>[] objects, Dictionary<string, bool> dic)
		{
			return objects.Where(x => dic.ContainsKey(x.Item1) ? dic[x.Item1] : true).Select(x => x.Item2).ToArray();
		}

		public IEnumerable<IResult> Print()
		{
			return Preview("Отчет по срокам годности", new ShelfLifeDocument(Items.Value.ToArray()));
		}

		private IEnumerable<IResult> Preview(string name, BaseDocument doc)
		{
			var docSettings = doc.Settings;
			if (docSettings != null) {
				yield return new DialogResult(new SimpleSettings(docSettings));
			}
			yield return new DialogResult(new PrintPreviewViewModel(new PrintResult(name, doc)), fullScreen: true);
		}

		public void SetMenuItems()
		{
			var item = new MenuItem {Header = "Отчет по срокам годности" };
			PrintStockMenuItems.Add(item);
		}

		PrintResult IPrintableStock.PrintStock()
		{
			var docs = new List<BaseDocument>();
			if (!IsView) {
				foreach (var item in PrintStockMenuItems.Where(i => i.IsChecked)) {
					if ((string) item.Header == "Отчет по срокам годности")
						docs.Add(new ShelfLifeDocument(Items.Value.ToArray()));
				}
				return new PrintResult(DisplayName, docs, PrinterName);
			}

			if(String.IsNullOrEmpty(LastOperation) || LastOperation == "Отчет по срокам годности")
				Coroutine.BeginExecute(Print().GetEnumerator());
			return null;
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
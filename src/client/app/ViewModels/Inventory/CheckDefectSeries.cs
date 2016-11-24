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
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using NHibernate.Mapping;
using MySql.Data.MySqlClient;
using Common.MySql;
using NHibernate;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class CheckDefectSeries : BaseScreen2, IPrintableStock
	{
		public NotifyValue<List<Stock>> Items { get; set; }
		public NotifyValue<Stock> CurrentItem { get; set; }
		public NotifyValue<bool> IsAll { get; set; }
		public NotifyValue<bool> IsPerhaps { get; set; }
		public NotifyValue<bool> IsDefective { get; set; }
		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		public NotifyValue<List<Tuple<uint,uint>>> Link { get; set; }


		public CheckDefectSeries()
		{
			DisplayName = "Проверка забракованных серий";
			IsAll = new NotifyValue<bool>(true);
			Begin = new NotifyValue<DateTime>(DateTime.Today.AddMonths(-3));
			End = new NotifyValue<DateTime>(DateTime.Today);

			PrintStockMenuItems = new ObservableCollection<MenuItem>();
			SetMenuItems();
			IsView = true;
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();
			RxQuery(CalcLinks).Subscribe(Link);

			Link.Changed()
				.Merge(Begin.Changed())
				.Merge(End.Changed())
				.Merge(IsPerhaps.Changed())
				.Merge(IsDefective.Changed())
				.SelectMany(_ => RxQuery(LoadItems))
				.Subscribe(Items);
		}

		public IEnumerable<IResult> EnterItems()
		{
			var stock = CurrentItem.Value;
			if (stock != null)
				yield return new DialogResult(new EditDefectSeries(stock, Link));
			Begin.Refresh();
		}

		public IEnumerable<IResult> DisplayItem()
		{
			var stock = CurrentItem.Value;
			if (stock != null)
				yield return new DialogResult(new EditStock(stock.Id));
			Begin.Refresh();
		}

		public List<Stock> LoadItems(IStatelessSession session)
		{
			// для всех с неизвестным статусом, что попали в Link, устанавливается статус Возможно, но не сохраняется в базе
			var ids = Link.Value.Select(x => x.Item1).Distinct().ToList();
			var items = Stock.AvailableStocks(session).OrderBy(y => y.Product).ToList();
			foreach (var item in items) {
				if (item.RejectStatus == RejectStatus.Unknown && ids.Contains(item.Id))
					item.RejectStatus = RejectStatus.Perhaps;
			}

			if (IsPerhaps)
				items = items.Where(x => x.RejectStatus == RejectStatus.Perhaps).ToList();
			else if (IsDefective)
				items = items.Where(x => x.RejectStatus == RejectStatus.Defective).ToList();

			var rejects = session.Query<Reject>()
				.Where(x => !x.Canceled && x.LetterDate >= Begin.Value && x.LetterDate < End.Value.AddDays(1))
				.Select(x => x.Id)
				.ToList();
			var filteredIds = Link.Value.Where(x => rejects.Contains(x.Item2)).Select(x => x.Item1).ToList();
			items = items.Where(x => filteredIds.Contains(x.Id)).ToList();

			return items;
		}

		public IResult ExportExcel()
		{
			var columns = new[] {"Штрихкод",
				"Товар",
				"Производитель",
				"Серия",
				"Кол-во",
				"Брак"};

			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("Экспорт");
			var row = 0;

			ExcelExporter.WriteRow(sheet, columns, row++);

			var rows = Items.Value.Select((o, i) => new object[] {
				o.Barcode,
				o.Product,
				o.Producer,
				o.SerialNumber,
				o.Quantity,
				o.RejectStatusName
			});

			ExcelExporter.WriteRows(sheet, rows, row);
			return ExcelExporter.Export(book);
		}

		public IEnumerable<IResult> PrintDefectStock()
		{
			LastOperation = "Товарные запасы";
			return Preview("Товарные запасы", new DefectStockDocument(Items.Value.ToArray()));
		}

		private IEnumerable<IResult> Preview(string name, BaseDocument doc)
		{
			var docSettings = doc.Settings;
			if (docSettings != null) {
				yield return new DialogResult(new SimpleSettings(docSettings));
			}
			yield return new DialogResult(new PrintPreviewViewModel(new PrintResult(name, doc)), fullScreen: true);
		}


		private List<Tuple<uint, uint>> CalcLinks(IStatelessSession session)
		{
			var result = Session.CreateSQLQuery(@"select s.Id as StockId, r.Id as RejectId " +
				"from Stocks s " +
				" join Rejects r on s.ProducerId = r.ProducerId and s.ProductId = r.ProductId and s.SerialNumber = r.Series " +
				"where r.Canceled = 0 " +
				" and s.ProducerId is not null " +
				" and s.ProductId is not null " +
				" and s.SerialNumber is not null " +
				"union all " +
				"select s.Id as StockId, r.Id as RejectId " +
				"from Stocks s " +
				" join Rejects r on s.ProductId = r.ProductId and s.SerialNumber = r.Series " +
				"where r.Canceled = 0 " +
				" and (s.ProducerId is null or r.ProducerId is null) " +
				" and s.ProductId is not null " +
				" and s.SerialNumber is not null " +
				"union all " +
				"select s.Id as StockId, r.Id as RejectId " +
				"from Stocks s " +
				" join Rejects r on s.Product = r.Product and s.SerialNumber = r.Series " +
				"where r.Canceled = 0 " +
				" and s.ProductId is null " +
				" and s.Product is not null " +
				" and s.SerialNumber is not null")
			.List<object[]>()
			.Select(x => Tuple.Create(Convert.ToUInt32(x[0]), Convert.ToUInt32(x[1])))
			.Distinct()
			.ToList();

			return result;
		}

		private void SetMenuItems()
		{
			PrintStockMenuItems.Clear();
			var item = new MenuItem();
			item.Header = "Товарные запасы";
			item.Click += (sender, args) => Coroutine.BeginExecute(PrintDefectStock().GetEnumerator());
			PrintStockMenuItems.Add(item);

			item = new MenuItem {Header = "Настройки"};
			item.Click += (sender, args) => Coroutine.BeginExecute(ReportSetting().GetEnumerator());
			PrintStockMenuItems.Add(item);

			foreach (var it in PrintStockMenuItems) {
				it.IsCheckable = false;
			}
		}

		PrintResult IPrintableStock.PrintStock()
		{
			var docs = new List<BaseDocument>();
			if (!IsView) {
				foreach (var item in PrintStockMenuItems.Where(i => i.IsChecked)) {
					if ((string) item.Header == "Чеки")
						docs.Add(new DefectStockDocument(Items.Value.ToArray()));
				}
				return new PrintResult(DisplayName, docs, PrinterName);
			}

			if(String.IsNullOrEmpty(LastOperation) || LastOperation == "Товарные запасы")
				Coroutine.BeginExecute(PrintDefectStock().GetEnumerator());
			return null;
		}

		public IEnumerable<IResult> ReportSetting()
		{
			var req = new ReportSetting();
			yield return new DialogResult(req);
			PrinterName = req.PrinterName;
			if (req.IsView) {
				IsView = true;
				SetMenuItems();
			}

			if (req.IsPrint) {
				IsView = false;
				DisablePreview();
			}
		}

		public void DisablePreview()
		{
			foreach (var item in PrintStockMenuItems) {
				if (item.Header != "Настройки") {
					RemoveRoutedEventHandlers(item, MenuItem.ClickEvent);
					item.IsCheckable = true;
				}
			}
		}

		public static void RemoveRoutedEventHandlers(UIElement element, RoutedEvent routedEvent)
		{
			var eventHandlersStoreProperty = typeof (UIElement).GetProperty(
				"EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic);
			object eventHandlersStore = eventHandlersStoreProperty.GetValue(element, null);

			if (eventHandlersStore == null)
				return;

			var getRoutedEventHandlers = eventHandlersStore.GetType().GetMethod(
				"GetRoutedEventHandlers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			var routedEventHandlers = (RoutedEventHandlerInfo[]) getRoutedEventHandlers.Invoke(
				eventHandlersStore, new object[] {routedEvent});

			foreach (var routedEventHandler in routedEventHandlers)
				element.RemoveHandler(routedEvent, routedEventHandler.Handler);
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
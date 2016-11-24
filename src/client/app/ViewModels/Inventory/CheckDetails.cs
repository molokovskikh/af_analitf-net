using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using NHibernate.Linq;
using NPOI.HSSF.UserModel;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class CheckDetails : BaseScreen2, IPrintableStock
	{
		private uint id;

		public CheckDetails()
		{
			DisplayName = "Чек";
			Lines = new NotifyValue<IList<CheckLine>>(new List<CheckLine>());

			PrintStockMenuItems = new ObservableCollection<MenuItem>();
			SetMenuItems();
			IsView = true;
		}

		public CheckDetails(uint id)
			: this()
		{
			this.id = id;
		}

		public NotifyValue<Check> Header { get; set; }
		public NotifyValue<CheckLine> CurrentLine { get; set; }
		public NotifyValue<IList<CheckLine>> Lines { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			RxQuery(s => s.Query<Check>()
					.Fetch(x => x.Department)
					.FirstOrDefault(y => y.Id == id))
				.Subscribe(Header);
			RxQuery(x => {
					return x.Query<CheckLine>().Where(y => y.CheckId == id)
						.ToList()
						.ToObservableCollection();
				})
				.Subscribe(Lines);
		}

		public IEnumerable<IResult> PrintCheckDetails()
		{
			LastOperation = "Чеки";
			return Preview("Чеки", new CheckDetailsDocument(Lines.Value.ToArray(), Header.Value));
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

		public IResult ExportExcel()
		{
			var columns = new[] {
				"№",
				"Штрих-код",
				"Название товара",
				"Количество",
				"Цена розничная",
				"Сумма розничная",
				"Сумма скидки",
				"Сумма с учетом скидки"
			};

			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("Экспорт");
			var row = 0;

			ExcelExporter.WriteRow(sheet, columns, row++);

			var rows = Lines.Value.Select((o, i) => new object[] {
				o.Id,
				o.Barcode,
				o.Product,
				o.Quantity,
				o.RetailCost,
				o.RetailSum,
				o.DiscontSum,
				o.Sum,
			});

			ExcelExporter.WriteRows(sheet, rows, row);

			return ExcelExporter.Export(book);
		}

		private void SetMenuItems()
		{
			PrintStockMenuItems.Clear();
			var item = new MenuItem();
			item.Header = "Чеки";
			item.Click += (sender, args) => Coroutine.BeginExecute(PrintCheckDetails().GetEnumerator());
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
						docs.Add(new CheckDetailsDocument(Lines.Value.ToArray(), Header.Value));
				}
				return new PrintResult(DisplayName, docs, PrinterName);
			}

			if(String.IsNullOrEmpty(LastOperation) || LastOperation == "Чеки")
				Coroutine.BeginExecute(PrintCheckDetails().GetEnumerator());
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;
using NPOI.HSSF.UserModel;
using System.Collections.ObjectModel;
using System.Printing;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class EditInventoryDoc : BaseScreen2, IPrintableStock
	{
		private string Name;

		private EditInventoryDoc()
		{
			Lines = new ReactiveCollection<InventoryDocLine>();
			Session.FlushMode = FlushMode.Never;
			Name = User?.FullName ?? "";

			PrintStockMenuItems = new ObservableCollection<MenuItem>();
			SetMenuItems();
			IsView = true;
		}

		public EditInventoryDoc(InventoryDoc doc)
			: this()
		{
			DisplayName = "Новый документ Излишки";
			InitDoc(doc);
			Lines.AddRange(doc.Lines);
		}

		public EditInventoryDoc(uint id)
			: this()
		{
			DisplayName = "Редактирование документа Излишки";
			InitDoc(Session.Load<InventoryDoc>(id));
			Lines.AddRange(Doc.Lines);
		}

		public InventoryDoc Doc { get; set; }
		public ReactiveCollection<InventoryDocLine> Lines { get; set; }
		public NotifyValue<InventoryDocLine> CurrentLine { get; set; }
		public NotifyValue<bool> CanAdd { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanEditLine { get; set; }
		public NotifyValue<bool> CanPost { get; set; }
		public NotifyValue<bool> CanUnPost { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			if (Doc.Id == 0)
				Doc.Address = Address;
		}

		protected override void OnDeactivate(bool close)
		{
			Save();
			base.OnDeactivate(close);
		}

		private void InitDoc(InventoryDoc doc)
		{
			Doc = doc;
			var docStatus = Doc.ObservableForProperty(x => x.Status, skipInitial: false);
			var editOrDelete = docStatus
				.CombineLatest(CurrentLine, (x, y) => y != null && x.Value == DocStatus.NotPosted);
			editOrDelete.Subscribe(CanEditLine);
			editOrDelete.Subscribe(CanDelete);
			docStatus.Subscribe(x => CanAdd.Value = x.Value == DocStatus.NotPosted);
			docStatus.Select(x => x.Value == DocStatus.NotPosted).Subscribe(CanPost);
			docStatus.Select(x => x.Value == DocStatus.Posted).Subscribe(CanUnPost);
		}

		public IEnumerable<IResult> Add()
		{
			var search = new StockSearch();
			yield return new DialogResult(search, resizable: true);
			var edit = new EditStock(search.CurrentItem)
			{
				EditMode = EditStock.Mode.EditQuantity
			};
			yield return new DialogResult(edit);
			var line = new InventoryDocLine(Session.Load<Stock>(edit.Stock.Id), edit.Stock.Quantity, Session);
			Lines.Add(line);
			Doc.Lines.Add(line);
			Doc.UpdateStat();
		}

		public void Delete()
		{
			// с поставки наружу
			Session.Save(CurrentLine.Value.Stock.CancelInventoryDoc(CurrentLine.Value.Quantity));
			Lines.Remove(CurrentLine.Value);
			Doc.Lines.Remove(CurrentLine.Value);
			Doc.UpdateStat();
		}

		public IEnumerable<IResult> EditLine()
		{
			if (!CanEditLine)
				yield break;
			var stock = Env.Query(s => s.Get<Stock>(CurrentLine.Value.Stock.Id)).Result;
			stock.Quantity = CurrentLine.Value.Quantity;
			var edit = new EditStock(stock)
			{
				EditMode = EditStock.Mode.EditQuantity
			};
			yield return new DialogResult(edit);
			// вернули старое кол-во, приняли новое
			CurrentLine.Value.UpdateQuantity(edit.Stock.Quantity, Session);
			Doc.UpdateStat();
		}

		public IEnumerable<IResult> EnterLine()
		{
			return EditLine();
		}

		public void Post()
		{
			Doc.Post();
			Save();
		}

		public void UnPost()
		{
			Doc.UnPost();
			Save();
		}

		private void Save()
		{
			Session.Save(Doc);
			Session.Flush();
			Bus.SendMessage(nameof(InventoryDoc), "db");
			Bus.SendMessage(nameof(Stock), "db");
		}

		public IResult ExportExcel()
		{
			Update();
			var columns = new[] {
						"№ п/п",
						"Товар",
						"Производитель",
						"Серия",
						"Срок годности",
						"Штрихкод",
						"Кол-во",
						"Цена розничная с НДС",
						"Сумма розничная с НДС",
					};

			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("Экспорт");
			var row = 0;

			ExcelExporter.WriteRow(sheet, columns, row++);

			var rows = Lines.Select((o, i) => new object[] {
						o.Id,
						o.Product,
						o.Producer,
						o.SerialNumber,
						o.Period,
						o.Barcode,
						o.Quantity,
						o.RetailCost,
						o.RetailSum,
					});

			ExcelExporter.WriteRows(sheet, rows, row);

			return ExcelExporter.Export(book);
		}

		public IEnumerable<IResult> Print()
		{
			LastOperation = "Излишки";
			return Preview("Излишки", new InventoryDocument(Lines.ToArray()));
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
			LastOperation = "Ярлыки";
			return new DialogResult(new PrintPreviewViewModel
			{
				DisplayName = "Ярлыки",
				Document = new StockPriceTagDocument(Lines.Cast<BaseStock>().ToList(), Name).Build()
			}, fullScreen: true);
		}

		public IEnumerable<IResult> PrintInventoryAct()
		{
			LastOperation = "Акт об излишках";
			return Preview("Акт об излишках", new InventoryActDocument(Lines.ToArray()));
		}

		private void SetMenuItems()
		{
			PrintStockMenuItems.Clear();
			var item = new MenuItem();
			item.Header = "Излишки";
			item.Click += (sender, args) => Coroutine.BeginExecute(Print().GetEnumerator());
			PrintStockMenuItems.Add(item);

			item = new MenuItem();
			item.Header = "Ярлыки";
			item.Click += (sender, args) => PrintStockPriceTags().Execute(null);
			PrintStockMenuItems.Add(item);

			item = new MenuItem();
			item.Header = "Акт об излишках";
			item.Click += (sender, args) => Coroutine.BeginExecute(PrintInventoryAct().GetEnumerator());
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
					if ((string) item.Header == "Излишки")
						docs.Add(new InventoryDocument(Lines.ToArray()));
					if ((string) item.Header == "Акт об излишках")
						docs.Add(new InventoryActDocument(Lines.ToArray()));
					if ((string) item.Header == "Ярлыки")
						PrintFixedDoc(new StockPriceTagDocument(Lines.Cast<BaseStock>().ToList(), Name).Build().DocumentPaginator);
				}
				return new PrintResult(DisplayName, docs, PrinterName);
			}

			if(String.IsNullOrEmpty(LastOperation) || LastOperation == "Излишки")
				Coroutine.BeginExecute(Print().GetEnumerator());
			if(LastOperation == "Ярлыки")
				PrintStockPriceTags().Execute(null);
			if(LastOperation == "Акт об излишках")
				Coroutine.BeginExecute(PrintInventoryAct().GetEnumerator());
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

		private void PrintFixedDoc(DocumentPaginator doc)
		{
			var dialog = new PrintDialog();
				if(!string.IsNullOrEmpty(PrinterName))
					dialog.PrintQueue = new PrintQueue(new PrintServer(), PrinterName);
			if (string.IsNullOrEmpty(PrinterName))
							dialog.ShowDialog();
			dialog.PrintDocument(doc, "Ценники");
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
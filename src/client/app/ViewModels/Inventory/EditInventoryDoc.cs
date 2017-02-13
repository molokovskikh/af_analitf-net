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
using ReactiveUI;
using NPOI.HSSF.UserModel;
using System.Collections.ObjectModel;
using System.Printing;
using System.Windows.Controls;
using System.Windows.Documents;
using AnalitF.Net.Client.ViewModels.Offers;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class EditInventoryDoc : BaseScreen2, IPrintableStock
	{
		private EditInventoryDoc()
		{
			Lines = new ReactiveCollection<InventoryDocLine>();
			Session.FlushMode = FlushMode.Never;
			PrintStockMenuItems = new ObservableCollection<MenuItem>();
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
		public NotifyValue<bool> CanAddFromCatalog { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
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
			editOrDelete.Subscribe(CanDelete);
			docStatus.Subscribe(x => CanAdd.Value = x.Value == DocStatus.NotPosted);
			docStatus.Subscribe(x => CanAddFromCatalog.Value = x.Value == DocStatus.NotPosted);
			docStatus.Select(x => x.Value == DocStatus.NotPosted).Subscribe(CanPost);
			docStatus.Select(x => x.Value == DocStatus.Posted).Subscribe(CanUnPost);
		}

		public IEnumerable<IResult> Add()
		{
			while (true) {
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
				Save();
			}
		}

		public IEnumerable<IResult> AddFromCatalog()
		{
			while (true) {
				var search = new AddStockFromCatalog(Session, Address);
				yield return new DialogResult(search);
				var edit = new EditStock(search.Item) {
					EditMode = EditStock.Mode.EditQuantity
				};
				yield return new DialogResult(edit);
				var line = new InventoryDocLine(Session.Load<Stock>(edit.Stock.Id), edit.Stock.Quantity, Session);
				Lines.Add(line);
				Doc.Lines.Add(line);
				Doc.UpdateStat();
				Save();
			}
		}

		public void Delete()
		{
			// с поставки наружу
			Session.Save(CurrentLine.Value.Stock.CancelInventoryDoc(CurrentLine.Value.Quantity));
			Doc.Lines.Remove(CurrentLine.Value);
			Doc.UpdateStat();
			Lines.Remove(CurrentLine.Value);
			Save();
		}

		public void UpdateQuantity(InventoryDocLine line, decimal oldQuantity)
		{
			if (Session == null)
				return;
			line.UpdateQuantity(oldQuantity, Session);
			Doc.UpdateStat();
			Save();
		}

		public void Post()
		{
			if (!Doc.Lines.Any()) {
				Manager.Warning("Пустой документ не может быть проведен");
				return;
			}
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
			if (Doc.Id == 0)
				Session.Save(Doc);
			else
				Session.Update(Doc);
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

		public void Tags()
		{
			var tags = Lines.Select(x => x.Stock.GeTagPrintable(User?.FullName)).ToList();
			Shell.Navigate(new Tags(null, tags));
		}

		public IEnumerable<IResult> PrintInventoryAct()
		{
			return Preview("Акт об излишках", new InventoryActDocument(Lines.ToArray()));
		}

		public void SetMenuItems()
		{
			PrintStockMenuItems.Clear();
			var item = new MenuItem {Header = "Излишки"};
			PrintStockMenuItems.Add(item);

			item = new MenuItem {Header = "Ярлыки"};
			PrintStockMenuItems.Add(item);

			item = new MenuItem {Header = "Акт об излишках"};
			PrintStockMenuItems.Add(item);
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
						Tags();
				}
				return new PrintResult(DisplayName, docs, PrinterName);
			}

			if(String.IsNullOrEmpty(LastOperation) || LastOperation == "Излишки")
				Coroutine.BeginExecute(Print().GetEnumerator());
			if(LastOperation == "Ярлыки")
				Tags();
			if(LastOperation == "Акт об излишках")
				Coroutine.BeginExecute(PrintInventoryAct().GetEnumerator());
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
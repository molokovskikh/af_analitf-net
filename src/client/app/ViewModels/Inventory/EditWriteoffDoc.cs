using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using NHibernate;
using NHibernate.Linq;
using NPOI.HSSF.UserModel;
using ReactiveUI;
using System.Windows;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class EditWriteoffDoc : BaseScreen2, IPrintableStock
	{
		private EditWriteoffDoc()
		{
			Lines = new ReactiveCollection<WriteoffLine>();
			Session.FlushMode = FlushMode.Never;

			PrintStockMenuItems = new ObservableCollection<MenuItem>();
			IsView = true;
		}

		public EditWriteoffDoc(WriteoffDoc doc)
			: this()
		{
			DisplayName = "Новое списание";
			InitDoc(doc);
			Lines.AddRange(doc.Lines);
		}

		public EditWriteoffDoc(uint id)
			: this()
		{
			DisplayName = "Редактирование списания";
			InitDoc(Session.Load<WriteoffDoc>(id));
			Lines.AddRange(Doc.Lines);
		}

		public WriteoffDoc Doc { get; set; }

		public ReactiveCollection<WriteoffLine> Lines { get; set; }
		public NotifyValue<WriteoffLine> CurrentLine { get; set; }
		public NotifyValue<WriteoffReason[]> Reasons { get; set; }

		public NotifyValue<bool> CanAdd { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanEditLine { get; set; }
		public NotifyValue<bool> CanPost { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			if (Doc.Id == 0)
				Doc.Address = Address;
			RxQuery(s => s.Query<WriteoffReason>().OrderBy(x => x.Name).ToArray())
				.Subscribe(Reasons);
		}

		protected override void OnDeactivate(bool close)
		{
			Save();
			base.OnDeactivate(close);
		}

		private void InitDoc(WriteoffDoc doc)
		{
			Doc = doc;
			var docStatus = Doc.ObservableForProperty(x => x.Status, skipInitial: false);
			var editOrDelete = docStatus
				.CombineLatest(CurrentLine, (x, y) => y != null && x.Value == DocStatus.NotPosted);
			editOrDelete.Subscribe(CanEditLine);
			editOrDelete.Subscribe(CanDelete);
			docStatus.Subscribe(x => CanAdd.Value = x.Value == DocStatus.NotPosted);
			docStatus.Select(x => x.Value == DocStatus.NotPosted).Subscribe(CanPost);
		}

		public IEnumerable<IResult> Add()
		{
			var search = new StockSearch();
			yield return new DialogResult(search, resizable: true);
			var edit = new EditStock(search.CurrentItem) {
				EditMode = EditStock.Mode.EditQuantity
			};
			yield return new DialogResult(edit);
			var line = new WriteoffLine(Session.Load<Stock>(edit.Stock.Id), edit.Stock.Quantity);
			Lines.Add(line);
			Doc.Lines.Add(line);
			Doc.UpdateStat();
		}

		public void Delete()
		{
			CurrentLine.Value.Stock.Release(CurrentLine.Value.Quantity);
			Doc.Lines.Remove(CurrentLine.Value);
			Doc.UpdateStat();
			Lines.Remove(CurrentLine.Value);
		}

		public IEnumerable<IResult> EditLine()
		{
			if (!CanEditLine)
				yield break;
			var stock = Env.Query(s => s.Get<Stock>(CurrentLine.Value.Stock.Id)).Result;
			stock.Quantity = CurrentLine.Value.Quantity;
			var edit = new EditStock(stock) {
				EditMode = EditStock.Mode.EditQuantity
			};
			yield return new DialogResult(edit);
			CurrentLine.Value.UpdateQuantity(edit.Stock.Quantity);
			Doc.UpdateStat();
		}

		public IEnumerable<IResult> EnterLine()
		{
			return EditLine();
		}

		public void Post()
		{
			if (!Doc.Lines.Any()) {
				Manager.Warning("Пустой документ не может быть проведен");
				return;
			}
			Doc.Post(Session);
			Save();
		}

		private void Save()
		{
			if (!IsValide(Doc))
				return;
			if (Doc.Id == 0)
				Session.Save(Doc);
			else
				Session.Update(Doc);
			Session.Flush();
			Bus.SendMessage(nameof(WriteoffDoc), "db");
			Bus.SendMessage(nameof(Stock), "db");
		}

		public IResult ExportExcel()
		{
			Update();
			var columns = new[] {
						"№№",
						"Товар",
						"Производитель",
						"Кол-во",
						"Цена закупки",
						"Цена розничная",
						"Сумма закупки",
						"Сумма розничная"
					};

			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("Экспорт");
			var row = 0;

			ExcelExporter.WriteRow(sheet, columns, row++);

			var rows = Lines.Select((o, i) => new object[] {
						o.Id,
						o.Product,
						o.Producer,
						o.Quantity,
						o.SupplierCostWithoutNds,
						o.RetailCost,
						o.SupplierSumWithoutNds,
						o.RetailSum,
					});

			ExcelExporter.WriteRows(sheet, rows, row);

			return ExcelExporter.Export(book);
		}

		public IEnumerable<IResult> Print()
		{
			return Preview("Списание", new WriteoffDocument(Lines.ToArray()));
		}

		public IEnumerable<IResult> PrintAct()
		{
			return Preview("Акт списания", new WriteoffActDocument(Lines.ToArray()));
		}

		public void SetMenuItems()
		{
			var item = new MenuItem {Header = "Списание"};
			PrintStockMenuItems.Add(item);

			item = new MenuItem {Header = "Акт списания"};
			PrintStockMenuItems.Add(item);
		}

		PrintResult IPrintableStock.PrintStock()
		{
			var docs = new List<BaseDocument>();
			if (!IsView) {
				foreach (var item in PrintStockMenuItems.Where(i => i.IsChecked)) {
					if ((string)item.Header == "Списание")
						docs.Add(new WriteoffDocument(Lines.ToArray()));
					if ((string)item.Header == "Акт списания")
						docs.Add(new WriteoffActDocument(Lines.ToArray()));
				}
				return new PrintResult(DisplayName, docs, PrinterName);
			}

			if(String.IsNullOrEmpty(LastOperation) || LastOperation == "Списание")
				Coroutine.BeginExecute(Print().GetEnumerator());
			if(LastOperation == "Акт списания")
				Coroutine.BeginExecute(PrintAct().GetEnumerator());
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
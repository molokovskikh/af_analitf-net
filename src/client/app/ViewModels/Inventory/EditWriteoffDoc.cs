using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class EditWriteoffDoc : BaseScreen2
	{
		private EditWriteoffDoc()
		{
			Lines = new ReactiveCollection<WriteoffLine>();
			Session.FlushMode = FlushMode.Never;
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

		public NotifyValue<bool> IsDocOpen { get; set; }
		public ReactiveCollection<WriteoffLine> Lines { get; set; }
		public NotifyValue<WriteoffLine> CurrentLine { get; set; }
		public NotifyValue<WriteoffReason[]> Reasons { get; set; }

		public NotifyValue<bool> CanAddLine { get; set; }
		public NotifyValue<bool> CanDeleteLine { get; set; }
		public NotifyValue<bool> CanEditLine { get; set; }
		public NotifyValue<bool> CanSave { get; set; }
		public NotifyValue<bool> CanCloseDoc { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			if (Doc.Id == 0)
				Doc.Address = Address;
			RxQuery(s => s.Query<WriteoffReason>().OrderBy(x => x.Name).ToArray())
				.Subscribe(Reasons);
		}

		private void InitDoc(WriteoffDoc doc)
		{
			Doc = doc;
			var docStatus = Doc.ObservableForProperty(x => x.Status, skipInitial: false);
			var editOrDelete = docStatus
				.CombineLatest(CurrentLine, (x, y) => y != null && x.Value == DocStatus.Opened);
			editOrDelete.Subscribe(CanEditLine);
			editOrDelete.Subscribe(CanDeleteLine);
			docStatus.Subscribe(x => CanAddLine.Value = x.Value == DocStatus.Opened);
			docStatus.Select(x => x.Value == DocStatus.Opened).Subscribe(IsDocOpen);
			docStatus.Select(x => x.Value == DocStatus.Opened).Subscribe(CanCloseDoc);
			docStatus.Select(x => x.Value == DocStatus.Opened).Subscribe(CanSave);
		}

		public IEnumerable<IResult> AddLine()
		{
			var search = new StockSearch();
			yield return new DialogResult(search);
			var edit = new EditStock(search.CurrentItem) {
				EditMode = EditStock.Mode.EditQuantity
			};
			yield return new DialogResult(edit);
			var line = new WriteoffLine(Session.Load<Stock>(edit.Stock.Id), edit.Stock.Quantity);
			Lines.Add(line);
			Doc.Lines.Add(line);
			Doc.UpdateStat();
		}

		public void DeleteLine()
		{
			CurrentLine.Value.Stock.Release(CurrentLine.Value.Quantity);
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

		public void CloseDoc()
		{
			Doc.Close(Session);
			Save();
		}

		public void Save()
		{
			if (!IsValide(Doc))
				return;
			Session.Save(Doc);
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

		private IEnumerable<IResult> Preview(string name, BaseDocument doc)
		{
			var docSettings = doc.Settings;
			if (docSettings != null)
			{
				yield return new DialogResult(new SimpleSettings(docSettings));
			}
			yield return new DialogResult(new PrintPreviewViewModel(new PrintResult(name, doc)), fullScreen: true);
		}
	}
}
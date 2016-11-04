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

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class EditInventoryDoc : BaseScreen2
	{
		private string Name;

		private EditInventoryDoc()
		{
			Lines = new ReactiveCollection<InventoryDocLine>();
			Session.FlushMode = FlushMode.Never;
			Name = User?.FullName ?? "";
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
			return new DialogResult(new PrintPreviewViewModel
			{
				DisplayName = "Ярлыки",
				Document = new StockPriceTagDocument(Lines.Cast<BaseStock>().ToList(), Name).Build()
			}, fullScreen: true);
		}

		public IEnumerable<IResult> PrintInventoryAct()
		{
			return Preview("Акт об излишках", new InventoryActDocument(Lines.ToArray()));
		}
	}
}
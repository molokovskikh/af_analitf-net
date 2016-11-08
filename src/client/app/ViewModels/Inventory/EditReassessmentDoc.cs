using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;
using AnalitF.Net.Client.Models.Print;
using NPOI.HSSF.UserModel;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class EditReassessmentDoc : BaseScreen2
	{
		private string Name;

		private EditReassessmentDoc()
		{
			Lines = new ReactiveCollection<ReassessmentLine>();
			Session.FlushMode = FlushMode.Never;
			Name = User?.FullName ?? "";
		}

		public EditReassessmentDoc(ReassessmentDoc doc)
			: this()
		{
			DisplayName = "Новая переоценка";
			InitDoc(doc);
			Lines.AddRange(doc.Lines);
		}

		public EditReassessmentDoc(uint id)
			: this()
		{
			DisplayName = "Редактирование переоценки";
			InitDoc(Session.Load<ReassessmentDoc>(id));
			Lines.AddRange(Doc.Lines);
		}

		public ReassessmentDoc Doc { get; set; }

		public ReactiveCollection<ReassessmentLine> Lines { get; set; }
		public NotifyValue<ReassessmentLine> CurrentLine { get; set; }

		public NotifyValue<bool> CanAdd { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanEditLine { get; set; }
		public NotifyValue<bool> CanPost { get; set; }
		public NotifyValue<bool> CanReasssessment { get; set; }

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

		private void InitDoc(ReassessmentDoc doc)
		{
			Doc = doc;
			var docStatus = Doc.ObservableForProperty(x => x.Status, skipInitial: false);
			var editOrDelete = docStatus
				.CombineLatest(CurrentLine, (x, y) => y != null && x.Value == DocStatus.NotPosted);
			editOrDelete.Subscribe(CanEditLine);
			editOrDelete.Subscribe(CanDelete);
			var opened = docStatus.Select(x => x.Value == DocStatus.NotPosted);
			opened.Subscribe(CanAdd);
			opened.Subscribe(CanPost);
			opened.Subscribe(CanReasssessment);
		}

		[Description("Переоценка")]
		public class ReassessmentSettings
		{
			public ReassessmentSettings(Settings settings)
			{
				Markup = 5;
				Rounding = settings.Rounding;
			}

			[Display(Name = "Наценка")]
			public decimal Markup { get; set; }

			[Display(Name = "Округление")]
			public Rounding Rounding { get; set; }
		}

		public IEnumerable<IResult> Reassessment()
		{
			var settings = new ReassessmentSettings(Settings);
			yield return new DialogResult(new SimpleSettings(settings));
			foreach(var line in Lines.Where(x => x.Selected)) {
				line.DstStock.Configure(Settings);
				line.DstStock.RetailCost = Stock.Round(line.SrcRetailCost * (1 + settings.Markup / 100m), Settings.Value.Rounding);
				line.RetailCost = line.DstStock.RetailCost;
				line.RetailMarkup = line.DstStock.RetailMarkup;
				line.DstStock.Settings = null;
				line.DstStock.WaybillSettings = null;
			}
			Doc.UpdateStat();
		}

		public IEnumerable<IResult> Add()
		{
			var search = new StockSearch();
			yield return new DialogResult(search);
			var srcStock = Session.Load<Stock>(search.CurrentItem.Value.Id);
			var dstStock = srcStock.Copy();
			var edit = new EditStock(dstStock) {
				EditMode = EditStock.Mode.EditRetailCostAndQuantity
			};
			yield return new DialogResult(edit);
			var line = new ReassessmentLine(srcStock, dstStock);
			Lines.Add(line);
			Doc.Lines.Add(line);
			Doc.UpdateStat();
		}

		public IEnumerable<IResult> Search()
		{
			var search = new StockGroupSearch();
			yield return new DialogResult(search);

			if (search.WasCancelled)
				yield break;

			var ids = search.Items.Value.Select(x => x.Id).ToList();
			var srcStocks = Session.Query<Stock>().Where(x => ids.Contains(x.Id)).ToList();
			foreach (var srcStock in srcStocks)
			{
				var dstStock = srcStock.Copy();
				var line = new ReassessmentLine(srcStock, dstStock);
				Lines.Add(line);
				Doc.Lines.Add(line);
			}
			Doc.UpdateStat();
		}

		public void Delete()
		{
			if (!CanDelete)
				return;
			Doc.DeleteLine(CurrentLine.Value);
			Lines.Remove(CurrentLine.Value);
			Doc.UpdateStat();
		}

		public IEnumerable<IResult> EditLine()
		{
			if (!CanEditLine)
				yield break;
			var line = CurrentLine.Value;
			var stock = line.DstStock.Copy();
			stock.Quantity = line.Quantity;
			var edit = new EditStock(stock) {
				EditMode = EditStock.Mode.EditRetailCostAndQuantity
			};
			yield return new DialogResult(edit);
			line.UpdateDst(edit.Stock);
			Doc.UpdateStat();
		}

		public IEnumerable<IResult> EnterLine()
		{
			return EditLine();
		}

		public void Post()
		{
			Doc.Post(Session);
			Save();
		}

		private void Save()
		{
			if (!IsValide(Doc))
				return;
			Session.Save(Doc);
			Session.Flush();
			Bus.SendMessage(nameof(ReassessmentDoc), "db");
			Bus.SendMessage(nameof(Stock), "db");
		}

		public IResult ExportExcel()
		{
			Update();
			var columns = new[] {
						"Наименование товара",
						"Производитель",
						"Кол-во",
						"Цена закупки",
						"Наценка списания, %",
						"Цена списания",
						"Сумма списания",
						"Наценка приходования, %",
						"Цена приходования",
						"Сумма приходования",
					};

			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("Экспорт");
			var row = 0;

			ExcelExporter.WriteRow(sheet, columns, row++);

			var rows = Lines.Select((o, i) => new object[] {
						o.Product,
						o.Producer,
						o.Quantity,
						o.SupplierCostWithoutNds,
						o.SrcRetailMarkup,
						o.SrcRetailCost,
						o.SrcRetailSum,
						o.RetailMarkup,
						o.RetailCost,
						o.RetailSum,
					});

			ExcelExporter.WriteRows(sheet, rows, row);

			return ExcelExporter.Export(book);
		}

		public IEnumerable<IResult> Print()
		{
			return Preview("Переоценка", new ReassessmentDocument(Lines.ToArray()));
		}

		public IEnumerable<IResult> PrintAct()
		{
			return Preview("Акт переоценки", new ReassessmentActDocument(Lines.ToArray()));
		}

		public IResult PrintStockPriceTags()
		{
			return new DialogResult(new PrintPreviewViewModel
			{
				DisplayName = "Ценники",
				Document = new StockPriceTagDocument(Lines.Cast<BaseStock>().ToList(), Name).Build()
			}, fullScreen: true);
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
﻿using System;
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
	public class EditDisplacementDoc : BaseScreen2, IPrintableStock
	{
		private string Name;

		private EditDisplacementDoc()
		{
			Name = User?.FullName ?? "";
			Lines = new ReactiveCollection<DisplacementLine>();
			Session.FlushMode = FlushMode.Never;

			PrintStockMenuItems = new ObservableCollection<MenuItem>();
			IsView = true;
		}

		public EditDisplacementDoc(DisplacementDoc doc)
			: this()
		{
			DisplayName = "Новое внутреннее перемещение";
			InitDoc(doc);
			Lines.AddRange(doc.Lines);
		}

		public EditDisplacementDoc(uint id)
			: this()
		{
			DisplayName = "Редактирование внутреннего перемещения " + Session.Load<DisplacementDoc>(id).Id;
			InitDoc(Session.Load<DisplacementDoc>(id));
			Lines.AddRange(Doc.Lines);
		}

		public DisplacementDoc Doc { get; set; }

		public ReactiveCollection<DisplacementLine> Lines { get; set; }
		public NotifyValue<DisplacementLine> CurrentLine { get; set; }

		public NotifyValue<bool> CanAdd { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanEditLine { get; set; }
		public NotifyValue<bool> CanPost { get; set; }
		public NotifyValue<bool> CanUnPost { get; set; }
		public NotifyValue<bool> CanEndDoc { get; set; }

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

		private void InitDoc(DisplacementDoc doc)
		{
			Doc = doc;
			var docStatus = Doc.ObservableForProperty(x => x.Status, skipInitial: false);
			var editOrDelete = docStatus
				.CombineLatest(CurrentLine, (x, y) => y != null && x.Value == DisplacementDocStatus.NotPosted);
			editOrDelete.Subscribe(CanEditLine);
			editOrDelete.Subscribe(CanDelete);
			docStatus.Subscribe(x => CanAdd.Value = x.Value == DisplacementDocStatus.NotPosted);
			docStatus.Select(x => x.Value == DisplacementDocStatus.NotPosted).Subscribe(CanPost);
			docStatus.Select(x => x.Value == DisplacementDocStatus.Posted).Subscribe(CanUnPost);
			docStatus.Select(x => x.Value == DisplacementDocStatus.Posted).Subscribe(CanEndDoc);
		}

		public IEnumerable<IResult> Add()
		{
			while (true) {
				if (!IsValide(Doc))
					yield break;

				var search = new StockSearch();
				yield return new DialogResult(search, false, true);
				var edit = new EditStock(search.CurrentItem) {
					EditMode = EditStock.Mode.EditQuantity
				};
				yield return new DialogResult(edit);

				var srcStock = Session.Load<Stock>(edit.Stock.Id);
				var dstStock = srcStock.Copy();
				dstStock.Address = Doc.DstAddress;
				dstStock.Quantity = dstStock.ReservedQuantity = dstStock.SupplyQuantity = 0;
				Session.Save(dstStock);

				var line = new DisplacementLine(srcStock, dstStock, edit.Stock.Quantity);
				Lines.Add(line);
				Doc.Lines.Add(line);
				Doc.UpdateStat();
				Save();
			}
		}

		public void Delete()
		{
			CurrentLine.Value.SrcStock.Release(CurrentLine.Value.Quantity);
			Lines.Remove(CurrentLine.Value);
			Doc.Lines.Remove(CurrentLine.Value);
			Doc.UpdateStat();
			Save();
		}

		public IEnumerable<IResult> EditLine()
		{
			if (!CanEditLine)
				yield break;
			var stock = Env.Query(s => s.Get<Stock>(CurrentLine.Value.SrcStock.Id)).Result;
			stock.Quantity = CurrentLine.Value.Quantity;
			var edit = new EditStock(stock)
			{
				EditMode = EditStock.Mode.EditQuantity
			};
			yield return new DialogResult(edit);
			CurrentLine.Value.UpdateQuantity(edit.Stock.Quantity);
			Save();
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

		public void UnPost()
		{
			Doc.UnPost(Session);
			Save();
		}

		public void EndDoc()
		{
			Doc.End(Session);
			Save();
		}

		private void Save()
		{
			if (!IsValide(Doc))
				return;
			Session.Save(Doc);
			Session.Flush();
			Bus.SendMessage(nameof(DisplacementDoc), "db");
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
						"Цена закупки с НДС",
						"Сумма закупки с НДС",
						"Серия",
						"Срок",
						"Штрихкод",
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
						o.SupplierCost,
						o.SupplierSum,
						o.SerialNumber,
						o.Period,
						o.Barcode,
					});

			ExcelExporter.WriteRows(sheet, rows, row);

			return ExcelExporter.Export(book);
		}

		public IEnumerable<IResult> Print()
		{
			return Preview("Печать документов", new DisplacementDocument(Lines.ToArray()));
		}

		public IEnumerable<IResult> PrintDisplacementDocumentWaybill()
		{
			return Preview("Внутреннее-перемещение", new DisplacementDocumentWaybill(Doc, Lines, Session.Query<WaybillSettings>().First()));
		}

		public IResult PrintStockPriceTags()
		{
			return new DialogResult(new PrintPreviewViewModel
			{
				DisplayName = "Ценники",
				Document = new StockPriceTagDocument(Lines.Cast<BaseStock>().ToList(), Name).Build()
			}, fullScreen: true);
		}

		public IEnumerable<IResult> PrintDisplacementWaybill()
		{
			var req = new RequirementWaybill();
			yield return new DialogResult(req);
			if (req.requirementWaybillName == null)
				yield break;
			yield return new DialogResult(new PrintPreviewViewModel(new PrintResult("Требование-накладная",
				new DisplacementWDocument(Doc, Lines, Session.Query<WaybillSettings>().First(), req.requirementWaybillName))), fullScreen: true);
		}

		public IEnumerable<IResult> PrintPriceNegotiationProtocol()
		{
			var req = new RequirementNegotiationProtocol();
			yield return new DialogResult(req);
			if (req.Fio == null)
				yield break;
			yield return new DialogResult(new PrintPreviewViewModel(new PrintResult("Протокол согласования цен ЖНВЛП",
				new PriceNegotiationProtocol(Doc, Lines, req.Fio))), fullScreen: true);
		}

		public IResult PrintStockRackingMaps()
		{
			var receivingOrders = Session.Query<Waybill>().ToList();
			return new DialogResult(new PrintPreviewViewModel
			{
				DisplayName = "Постеллажная карта",
				Document = new StockRackingMapDocument(receivingOrders, Lines.Select(x => x.SrcStock).ToList()).Build()
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

		public void SetMenuItems()
		{
			var item = new MenuItem {Header = "Печать документов"};
			PrintStockMenuItems.Add(item);

			item = new MenuItem {Header = "Ценники"};
			PrintStockMenuItems.Add(item);

			item = new MenuItem {Header = "Постеллажная карта"};
			PrintStockMenuItems.Add(item);

			item = new MenuItem {Header = "Требование-накладная"};
			PrintStockMenuItems.Add(item);

			item = new MenuItem {Header = "Внутреннее-перемещение"};
			PrintStockMenuItems.Add(item);

			item = new MenuItem {Header = "Протокол согласования цен ЖНВЛП"};
			PrintStockMenuItems.Add(item);
		}

		PrintResult IPrintableStock.PrintStock()
		{
			var docs = new List<BaseDocument>();
			if (!IsView) {
				foreach (var item in PrintStockMenuItems.Where(i => i.IsChecked)) {
					if ((string) item.Header == "Печать документов")
						docs.Add(new DisplacementDocument(Lines.ToArray()));
					if ((string) item.Header == "Печать ярлыков")
						PrintFixedDoc(new StockPriceTagDocument(Lines.Cast<BaseStock>().ToList(), Name).Build().DocumentPaginator,
							"Печать ярлыков");
					if ((string) item.Header == "Постеллажная карта") {
						var receivingOrders = Session.Query<Waybill>().ToList();
						PrintFixedDoc(
							new StockRackingMapDocument(receivingOrders, Lines.Select(x => x.SrcStock).ToList()).Build().DocumentPaginator,
							"Постеллажная карта");
					}
					if ((string) item.Header == "Требование-накладная") {
						var req = new RequirementWaybill();
						var reqDialog = new DialogResult(req);
						reqDialog.Execute(null);
						if (req.requirementWaybillName != null)
							docs.Add(new DisplacementWDocument(Doc, Lines, Session.Query<WaybillSettings>().First(),
								req.requirementWaybillName));
					}
					if ((string) item.Header == "Внутреннее-перемещение")
						docs.Add(new DisplacementDocumentWaybill(Doc, Lines, Session.Query<WaybillSettings>().First()));
					if ((string) item.Header == "Протокол согласования цен ЖНВЛП") {
						var req = new RequirementNegotiationProtocol();
						var reqDialog = new DialogResult(req);
						reqDialog.Execute(null);
						if (req.Fio != null)
							docs.Add(new PriceNegotiationProtocol(Doc, Lines, req.Fio));
					}
				}
				return new PrintResult(DisplayName, docs, PrinterName);
			}

			if(String.IsNullOrEmpty(LastOperation) || LastOperation == "Печать документов")
				Coroutine.BeginExecute(Print().GetEnumerator());
			if(LastOperation == "Печать ярлыков")
				PrintStockPriceTags().Execute(null);
			if(LastOperation == "Постеллажная карта")
				PrintStockRackingMaps().Execute(null);
			if(LastOperation == "Требование-накладная")
				Coroutine.BeginExecute(PrintDisplacementWaybill().GetEnumerator());
			if(LastOperation == "Внутреннее-перемещение")
				Coroutine.BeginExecute(PrintDisplacementDocumentWaybill().GetEnumerator());
			if(LastOperation == "Протокол согласования цен ЖНВЛП")
				Coroutine.BeginExecute(PrintPriceNegotiationProtocol().GetEnumerator());
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
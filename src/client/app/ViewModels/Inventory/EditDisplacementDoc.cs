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
	public class EditDisplacementDoc : BaseScreen2
	{
		private string Name;

		private EditDisplacementDoc()
		{
			Name = User?.FullName ?? "";
			Lines = new ReactiveCollection<DisplacementLine>();
			Session.FlushMode = FlushMode.Never;
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
			DisplayName = "Редактирование внутреннего перемещения";
			InitDoc(Session.Load<DisplacementDoc>(id));
			Lines.AddRange(Doc.Lines);
		}

		public DisplacementDoc Doc { get; set; }

		public NotifyValue<bool> IsDocOpen { get; set; }
		public ReactiveCollection<DisplacementLine> Lines { get; set; }
		public NotifyValue<DisplacementLine> CurrentLine { get; set; }

		public NotifyValue<bool> CanAddLine { get; set; }
		public NotifyValue<bool> CanDeleteLine { get; set; }
		public NotifyValue<bool> CanEditLine { get; set; }
		public NotifyValue<bool> CanSave { get; set; }
		public NotifyValue<bool> CanCloseDoc { get; set; }
		public NotifyValue<bool> CanReOpenDoc { get; set; }
		public NotifyValue<bool> CanEndDoc { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			if (Doc.Id == 0)
				Doc.Address = Address;
		}

		private void InitDoc(DisplacementDoc doc)
		{
			Doc = doc;
			var docStatus = Doc.ObservableForProperty(x => x.Status, skipInitial: false);
			var editOrDelete = docStatus
				.CombineLatest(CurrentLine, (x, y) => y != null && x.Value == DisplacementDocStatus.Opened);
			editOrDelete.Subscribe(CanEditLine);
			editOrDelete.Subscribe(CanDeleteLine);
			docStatus.Subscribe(x => CanAddLine.Value = x.Value == DisplacementDocStatus.Opened);
			docStatus.Select(x => x.Value == DisplacementDocStatus.Opened).Subscribe(IsDocOpen);
			docStatus.Select(x => x.Value == DisplacementDocStatus.Opened).Subscribe(CanCloseDoc);
			docStatus.Select(x => x.Value == DisplacementDocStatus.Opened).Subscribe(CanSave);
			docStatus.Select(x => x.Value == DisplacementDocStatus.Closed).Subscribe(CanReOpenDoc);
			docStatus.Select(x => x.Value == DisplacementDocStatus.Closed).Subscribe(CanEndDoc);
		}

		public IEnumerable<IResult> AddLine()
		{
			if (!IsValide(Doc))
				yield break;

			var search = new StockSearch();
			yield return new DialogResult(search);
			var edit = new EditStock(search.CurrentItem)
			{
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
		}

		public void DeleteLine()
		{
			CurrentLine.Value.SrcStock.Release(CurrentLine.Value.Quantity);
			Lines.Remove(CurrentLine.Value);
			Doc.Lines.Remove(CurrentLine.Value);
			Doc.UpdateStat();
		}

		public IEnumerable<IResult> EditLine()
		{
			if (!CanEditLine)
				yield break;
			var stock = StatelessSession.Get<Stock>(CurrentLine.Value.SrcStock.Id);
			stock.Quantity = CurrentLine.Value.Quantity;
			var edit = new EditStock(stock)
			{
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

		public void ReOpenDoc()
		{
			Doc.ReOpen(Session);
			Save();
		}

		public void EndDoc()
		{
			Doc.End(Session);
			Save();
		}

		public void Save()
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
			return Preview("Внутренее перемещение", new DisplacementDocument(Lines.ToArray()));
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
			return Preview("Требование-накладная", new DisplacementWDocument(Doc, Lines));
		}

		public IResult PrintStockRackingMaps()
		{
			var receivingOrders = Session.Query<ReceivingOrder>().ToList();

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
	}
}

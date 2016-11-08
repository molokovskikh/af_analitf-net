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

		public void Delete()
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
			var stock = Env.Query(s => s.Get<Stock>(CurrentLine.Value.SrcStock.Id)).Result;
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
	}
}

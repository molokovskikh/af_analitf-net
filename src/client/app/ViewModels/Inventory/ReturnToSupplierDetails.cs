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
	public class ReturnToSupplierDetails : BaseScreen2
	{
		private ReturnToSupplierDetails()
		{
			Lines = new ReactiveCollection<ReturnToSupplierLine>();
			Session.FlushMode = FlushMode.Never;
		}

		public ReturnToSupplierDetails(ReturnToSupplier doc)
			: this()
		{
			DisplayName = "Новый возврат поставщику";
			InitDoc(doc);
			Lines.AddRange(doc.Lines);
		}

		public ReturnToSupplierDetails(uint id)
			: this()
		{
			DisplayName = "Редактирование возврата поставщику";
			InitDoc(Session.Load<ReturnToSupplier>(id));
			Lines.AddRange(Doc.Lines);
		}

		public ReturnToSupplier Doc { get; set; }

		public NotifyValue<bool> IsDocOpen { get; set; }
		public ReactiveCollection<ReturnToSupplierLine> Lines { get; set; }
		public NotifyValue<ReturnToSupplierLine> CurrentLine { get; set; }
		public Supplier[] Suppliers { get; set; }

		public NotifyValue<bool> CanAddLine { get; set; }
		public NotifyValue<bool> CanDeleteLine { get; set; }
		public NotifyValue<bool> CanEditLine { get; set; }
		public NotifyValue<bool> CanSave { get; set; }
		public NotifyValue<bool> CanCloseDoc { get; set; }
		public NotifyValue<bool> CanReOpenDoc { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			Suppliers = Session.Query<Supplier>().OrderBy(x => x.Name).ToArray();
			if (Doc.Id == 0)
				Doc.Address = Address;
		}

		private void InitDoc(ReturnToSupplier doc)
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
			docStatus.Select(x => x.Value == DocStatus.Closed).Subscribe(CanReOpenDoc);
		}

		public IEnumerable<IResult> AddLine()
		{
			var search = new StockSearch();
			yield return new DialogResult(search);
			var edit = new EditStock(search.CurrentItem)
			{
				EditMode = EditStock.Mode.EditQuantity
			};
			yield return new DialogResult(edit);
			var line = new ReturnToSupplierLine(Session.Load<Stock>(edit.Stock.Id), edit.Stock.Quantity);
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
			var stock = StatelessSession.Get<Stock>(CurrentLine.Value.Stock.Id);
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

		public void Save()
		{
			if (!IsValide(Doc))
				return;
			Session.Save(Doc);
			Session.Flush();
			Bus.SendMessage(nameof(ReturnToSupplier), "db");
			Bus.SendMessage(nameof(Stock), "db");
		}

		public IResult ExportExcel()
		{
			Update();
			var columns = new[] {
						"№№",
						"Товар",
						"Производитель",
						"Количество",
						"Цена закупки",
						"Цена закупки с НДС",
						"Цена розничная",
						"Сумма закупки",
						"Сумма закупки с НДС",
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
						o.SupplierCost,
						o.RetailCost,
						o.SupplierSumWithoutNds,
						o.SupplierSum,
						o.RetailSum,
					});

			ExcelExporter.WriteRows(sheet, rows, row);

			return ExcelExporter.Export(book);
		}

		public IEnumerable<IResult> Print()
		{
			return Preview("Возврат товара", new ReturnToSuppliersDetailsDocument(Lines.ToArray()));
		}

		public IEnumerable<IResult> PrintReturnLabel()
		{
			return Preview("Возврат ярлык", new ReturnLabel(Doc, Session.Query<WaybillSettings>().First()));
		}

		public IEnumerable<IResult> PrintReturnInvoice()
		{
			return Preview("Возврат счет-фактура", new ReturnInvoice(Doc, Session.Query<WaybillSettings>().First()));
		}

		public IEnumerable<IResult> PrintReturnWaybill()
		{
			return Preview("Возврат товарная накладная", new ReturnWaybill(Doc, Session.Query<WaybillSettings>().First()));
		}

		public IEnumerable<IResult> PrintReturnDivergenceAct()
		{
			return Preview("Возврат акт о расхождении",
				new ReturnDivergenceAct(Doc, Session.Query<WaybillSettings>().First()));
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

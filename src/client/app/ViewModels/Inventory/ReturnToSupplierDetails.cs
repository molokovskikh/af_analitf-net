using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
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

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class ReturnToSupplierDetails : BaseScreen2, IPrintableStock
	{
		private ReturnToSupplierDetails()
		{
			Lines = new ReactiveCollection<ReturnToSupplierLine>();
			Session.FlushMode = FlushMode.Never;
			SetMenuItems();
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
			DisplayName = "Редактирование возврата поставщику " + id;
			InitDoc(Session.Load<ReturnToSupplier>(id));
			Lines.AddRange(Doc.Lines);
		}

		public ReturnToSupplier Doc { get; set; }

		public ReactiveCollection<ReturnToSupplierLine> Lines { get; set; }
		public NotifyValue<ReturnToSupplierLine> CurrentLine { get; set; }
		public Supplier[] Suppliers { get; set; }

		public NotifyValue<bool> CanAdd { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanEditLine { get; set; }
		public NotifyValue<bool> CanPost { get; set; }
		public NotifyValue<bool> CanUnPost { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			Suppliers = Session.Query<Supplier>().OrderBy(x => x.Name).ToArray();
			if (Doc.Id == 0)
				Doc.Address = Address;
		}

		protected override void OnDeactivate(bool close)
		{
			Save();
			base.OnDeactivate(close);
		}

		private void InitDoc(ReturnToSupplier doc)
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
			while (true) {
				var search = new StockSearch();
			yield return new DialogResult(search, false, true);
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
		}

		public void Delete()
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

		private void Save()
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
			LastOperation = "Возврат товара";
			return Preview("Возврат товара", new ReturnToSuppliersDetailsDocument(Lines.ToArray(), Doc, Session.Query<WaybillSettings>().First()));
		}

		public IEnumerable<IResult> PrintReturnLabel()
		{
			LastOperation = "Возврат ярлык";
			return Preview("Возврат ярлык", new ReturnLabel(Doc, Session.Query<WaybillSettings>().First()));
		}

		public IEnumerable<IResult> PrintReturnInvoice()
		{
			LastOperation = "Возврат счет-фактура";
			return Preview("Возврат счет-фактура", new ReturnInvoice(Doc, Session.Query<WaybillSettings>().First()));
		}

		public IEnumerable<IResult> PrintReturnWaybill()
		{
			LastOperation = "Возврат товарная накладная";
			return Preview("Возврат товарная накладная ТОРГ-12", new ReturnWaybill(Doc,
				Session.Query<WaybillSettings>().First(),
				Session.Query<User>().First()));
		}

		public IEnumerable<IResult> PrintReturnDivergenceAct()
		{
			LastOperation = "Акт о расхождении";
			return Preview("Акт о расхождении ТОРГ-2",
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

		private void SetMenuItems()
		{
			PrintStockMenuItems = new ObservableCollection<MenuItem>();
			var item = new MenuItem();
			item.Header = "Возврат товара";
			item.Click += (sender, args) => Coroutine.BeginExecute(Print().GetEnumerator());
			PrintStockMenuItems.Add(item);

			item = new MenuItem();
			item.Header = "Возврат ярлык";
			item.Click += (sender, args) => Coroutine.BeginExecute(PrintReturnLabel().GetEnumerator());
			PrintStockMenuItems.Add(item);

			item = new MenuItem();
			item.Header = "Возврат счет-фактура";
			item.Click += (sender, args) => Coroutine.BeginExecute(PrintReturnInvoice().GetEnumerator());
			PrintStockMenuItems.Add(item);

			item = new MenuItem();
			item.Header = "Возврат товарная накладная ТОРГ-12";
			item.Click += (sender, args) => Coroutine.BeginExecute(PrintReturnWaybill().GetEnumerator());
			PrintStockMenuItems.Add(item);

			item = new MenuItem();
			item.Header = "Акт о расхождении ТОРГ-2";
			item.Click += (sender, args) => Coroutine.BeginExecute(PrintReturnDivergenceAct().GetEnumerator());
			PrintStockMenuItems.Add(item);
		}

		void IPrintableStock.PrintStock()
		{
			if(String.IsNullOrEmpty(LastOperation) || LastOperation == "Возврат товара")
				Coroutine.BeginExecute(Print().GetEnumerator());
			if(LastOperation == "Возврат ярлык")
				 Coroutine.BeginExecute(PrintReturnLabel().GetEnumerator());
			if(LastOperation == "Возврат счет-фактура")
				Coroutine.BeginExecute(PrintReturnInvoice().GetEnumerator());
			if(LastOperation == "Возврат товарная накладная")
				Coroutine.BeginExecute(PrintReturnWaybill().GetEnumerator());
			if(LastOperation == "Акт о расхождении")
				Coroutine.BeginExecute(PrintReturnDivergenceAct().GetEnumerator());
		}

		public ObservableCollection<MenuItem> PrintStockMenuItems { get; set; }
		public string LastOperation { get; set; }
		public bool CanPrintStock
		{
			get { return true; }
		}
	}
}

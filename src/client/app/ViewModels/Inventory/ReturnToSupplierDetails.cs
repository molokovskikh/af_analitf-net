using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using NPOI.HSSF.UserModel;
using System.ComponentModel;
using System.Reactive;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class ReturnToSupplierDetails : BaseScreen2
	{
		public ReturnToSupplierDetails()
		{
			DisplayName = "Детализация Возврат поставщику";
			SelectedItems = new List<ReturnToSupplierLine>();
			CanDelete = CurrentItem.Select(v => v != null).ToValue();
		}

		public ReturnToSupplierDetails(uint id) : this()
		{
			var returnToSupplier = Session.Query<ReturnToSupplier>().SingleOrDefault(x => x.Id == id);
			ReturnToSupplier.Value = returnToSupplier;
			if (returnToSupplier != null)
			{
				DisplayName = $"Возврат №{returnToSupplier.NumDoc} от {returnToSupplier.DateDoc.ToString("dd.MM.yyyy")}";
				Items.Value = returnToSupplier.Lines;
			}
		}

		[Export]
		public NotifyValue<IList<ReturnToSupplierLine>> Items { get; set; }
		public NotifyValue<ReturnToSupplierLine> CurrentItem { get; set; }
		public List<ReturnToSupplierLine> SelectedItems { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<ReturnToSupplier> ReturnToSupplier { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
		}

		protected override void OnDeactivate(bool close)
		{
			base.OnDeactivate(close);
		}

		public IEnumerable<IResult> EnterItem()
		{
			if (CurrentItem.Value == null)
				yield break;

			var returnToSupplierLine = Session.Query<ReturnToSupplierLine>().Single(x => x.Id == CurrentItem.Value.Id);
			yield return new DialogResult(new CreateReturnToSupplierLine(returnToSupplierLine));

			var stock = returnToSupplierLine.Stock;
			returnToSupplierLine.Producer = stock.Producer;
			returnToSupplierLine.ProducerId = stock.ProducerId;
			returnToSupplierLine.Product = stock.Product;
			returnToSupplierLine.ProductId = stock.ProductId;
			returnToSupplierLine.RetailCost = stock.RetailCost;
			returnToSupplierLine.SupplierCost = stock.SupplierCost;
			returnToSupplierLine.SupplierCostWithoutNds = stock.SupplierCostWithoutNds;

			Session.Update(returnToSupplierLine);
			Session.Flush();
			Update();
		}

		public void Delete()
		{
			if (!CanDelete)
				return;

			if (!Confirm("Удалить выбранные строки?"))
				return;

			foreach (var item in SelectedItems.ToArray())
			{
				Items.Value.Remove(item);
				StatelessSession.Delete(item);
				Items.Refresh();
			}
		}

		public void Checkout()
		{
			if (ReturnToSupplier.Value == null || Items.Value == null || ReturnToSupplier.Value.Status != Status.Open)
				return;

			using (var trx = StatelessSession.BeginTransaction())
			{
				foreach (var item in Items.Value)
				{
					var stock = Session.Query<Stock>().SingleOrDefault(x => x.Id == item.Stock.Id);
					if (stock == null || stock.Quantity < item.Quantity)
						return;
					stock.Quantity -= item.Quantity;
					if (stock.Quantity == 0)
						Session.Delete(stock);
					else
						Session.Update(stock);
				}
				var returnToSupplier = Session.Query<ReturnToSupplier>().Single(x => x.Id == ReturnToSupplier.Value.Id);
				returnToSupplier.Status = Status.Closed;
				Session.Update(returnToSupplier);
				trx.Commit();
			}
			Bus.SendMessage("Stocks", "db");
		}

		private IQueryable<Stock> StockQuery()
		{
			return Stock.AvailableStocks(StatelessSession, Address);
		}

		public IEnumerable<IResult> Create()
		{
			var returnToSupplierLine = new ReturnToSupplierLine(ReturnToSupplier.Value.Id);
			yield return new DialogResult(new CreateReturnToSupplierLine(returnToSupplierLine));

			var stock = returnToSupplierLine.Stock;
			returnToSupplierLine.Producer = stock.Producer;
			returnToSupplierLine.ProducerId = stock.ProducerId;
			returnToSupplierLine.Product = stock.Product;
			returnToSupplierLine.ProductId = stock.ProductId;
			returnToSupplierLine.RetailCost = stock.RetailCost;
			returnToSupplierLine.SupplierCost = stock.SupplierCost;
			returnToSupplierLine.SupplierCostWithoutNds = stock.SupplierCostWithoutNds;

			Session.Save(returnToSupplierLine);
			Update();
		}

		public override void Update()
		{
			if (ReturnToSupplier.Value == null)
				return;

			RxQuery(x => {
				return x.Query<ReturnToSupplierLine>()
					.Where(y => y.ReturnToSupplierId == ReturnToSupplier.Value.Id)
					.ToList()
					.ToObservableCollection();
			})
			.Subscribe(Items);
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

			var rows = Items.Value.Select((o, i) => new object[] {
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
			return Preview("Возврат товара", new ReturnToSuppliersDetailsDocument(Items.Value.ToArray()));
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

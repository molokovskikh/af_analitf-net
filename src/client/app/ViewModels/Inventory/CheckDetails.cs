using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using NHibernate.Linq;
using NPOI.HSSF.UserModel;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class CheckDetails : BaseScreen2, IPrintable
	{
		private uint id;

		public CheckDetails()
		{
			DisplayName = "Чек";
			Lines = new NotifyValue<IList<CheckLine>>(new List<CheckLine>());

			PrintMenuItems = new ObservableCollection<MenuItem>();
			IsView = true;
		}

		public CheckDetails(uint id)
			: this()
		{
			this.id = id;
		}

		public NotifyValue<Check> Header { get; set; }
		public NotifyValue<CheckLine> CurrentLine { get; set; }
		public NotifyValue<IList<CheckLine>> Lines { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			RxQuery(s => s.Query<Check>()
					.Fetch(x => x.Address)
					.FirstOrDefault(y => y.Id == id))
				.Subscribe(Header);
			RxQuery(x => {
					return x.Query<CheckLine>().Where(y => y.CheckId == id)
						.ToList()
						.ToObservableCollection();
				})
				.Subscribe(Lines);
		}

		public IEnumerable<IResult> PrintCheckDetails()
		{
			LastOperation = DisplayName;
			return Preview(DisplayName, new CheckDetailsDocument(Lines.Value.ToArray(), Header.Value));
		}

		public IResult ExportExcel()
		{
			var columns = new[] {
				"№",
				"Штрих-код",
				"Название товара",
				"Количество",
				"Цена розничная",
				"Сумма розничная",
				"Сумма скидки",
				"Сумма с учетом скидки"
			};

			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("Экспорт");
			var row = 0;

			ExcelExporter.WriteRow(sheet, columns, row++);

			var rows = Lines.Value.Select((o, i) => new object[] {
				o.Id,
				o.Barcode,
				o.Product,
				o.Quantity,
				o.RetailCost,
				o.RetailSum,
				o.DiscontSum,
				o.Sum,
			});

			ExcelExporter.WriteRows(sheet, rows, row);

			return ExcelExporter.Export(book);
		}

		public void SetMenuItems()
		{
			var item = new MenuItem {Header = DisplayName};
			PrintMenuItems.Add(item);
		}

		PrintResult IPrintable.Print()
		{
			var docs = new List<BaseDocument>();
			if (!IsView) {
				var printItems = PrintMenuItems.Where(i => i.IsChecked).ToList();
				if (!printItems.Any())
					printItems.Add(PrintMenuItems.First());
				foreach (var item in printItems) {
					if ((string) item.Header == DisplayName)
						docs.Add(new CheckDetailsDocument(Lines.Value.ToArray(), Header.Value));
				}
				return new PrintResult(DisplayName, docs, PrinterName);
			}

			if(String.IsNullOrEmpty(LastOperation) || LastOperation == DisplayName)
				Coroutine.BeginExecute(PrintCheckDetails().GetEnumerator());
			return null;
		}

		public ObservableCollection<MenuItem> PrintMenuItems { get; set; }
		public string LastOperation { get; set; }
		public string PrinterName { get; set; }
		public bool IsView { get; set; }

		public bool CanPrint
		{
			get { return true; }
		}
	}
}

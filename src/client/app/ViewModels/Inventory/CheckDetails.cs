using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using NPOI.HSSF.UserModel;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class CheckDetails : BaseScreen2
	{
		private uint id;

		public CheckDetails()
		{
			DisplayName = "Чек";
		}

		public CheckDetails(Check header)
			: this()
		{
			Header.Value = header;
		}

		public NotifyValue<Check> Header { get; set; }
		public NotifyValue<CheckLine> CurrentLine { get; set; }
		public NotifyValue<IList<CheckLine>> Lines { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			Lines.Value = Header.Value.Lines;
		}
		public IEnumerable<IResult> PrintCheckDetails()
		{
			return Preview("Чеки", new CheckDetailsDocument(Lines.Value.ToArray(), Header.Value));
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

		public IResult ExportExcel()
		{
			var columns = new[] {"№",
				"Штрих-код",
				"Название товара",
				"Количество",
				"Цена розничная",
				"Сумма розничная",
				"Сумма скидки",
				"Сумма с учетом скидки"};

			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("Экспорт");
			var row = 0;

			ExcelExporter.WriteRow(sheet, columns, row++);

			var rows = Lines.Value.Select((o, i) => new object[] {
				o.Id,
				o.Barcode,
				o.ProductName,
				o.Quantity,
				o.RetailCost,
				o.RetailSum,
				o.DiscontSum,
				o.Sum,
			});

			ExcelExporter.WriteRows(sheet, rows, row);

			return ExcelExporter.Export(book);
		}
	}
}

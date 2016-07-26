using System;
using System.Collections.Generic;
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

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	class Checks : BaseScreen2
	{
		private Main main;

		public Checks()
		{
			Begin.Value = DateTime.Today.AddDays(-7);
			End.Value = DateTime.Today;
			ChangeDate.Value = DateTime.Today;
			SearchBehavior = new SearchBehavior(this);
			Items = new NotifyValue<IList<Check>>(new List<Check>());
			KKMFilter = new NotifyValue<IList<Selectable<string>>>(new List<Selectable<string>>());
		}

		public Checks(Main main)
			: this()
		{
			this.main = main;
		}

		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		public NotifyValue<DateTime> ChangeDate { get; set; }
		public SearchBehavior SearchBehavior { get; set; }
		public NotifyValue<IList<Check>> Items { get; set; }
		public NotifyValue<Check> CurrentItem { get; set; }
		public NotifyValue<IList<Selectable<Address>>> AddressesFilter { get; set; }
		public NotifyValue<IList<Selectable<string>>> KKMFilter { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			RxQuery(x => x.Query<Address>().OrderBy(y => y.Name).ToArray().Select(y => new Selectable<Address>(y)).ToList())
				.Subscribe(AddressesFilter);
			SampleData();
		}
		public void EnterItem()
		{
			if (CurrentItem.Value == null)
				return;
			main.ActiveItem = new CheckDetails(CurrentItem.Value);
		}

		private IList<Check> TempFillItemsList()
		{
			var checks = new List<Check>();
			var check = new Check(0);
			check.Lines = new List<CheckLine>();
			check.Lines.Add(new CheckLine());
			checks.Add(check);
			return checks;
		}

		private void SampleData()
		{
			Items.Value = TempFillItemsList();
			KKMFilter.Value.Add(new Selectable<string>("1(0000000)"));
		}

		public void Filter()
		{
			IEnumerable<Check> items = Items.Value;
			var term = SearchBehavior.ActiveSearchTerm.Value;
			if (!String.IsNullOrEmpty(term)) {
				items = items.Where(o => o.ChangeNumber.ToString() == term);
			}
			Items.Value = items.OrderBy(o => o.ChangeNumber).ToList();
		}

		public IEnumerable<IResult> PrintChecks()
		{
			return Preview("Чеки", new CheckDocument(Items.Value.ToArray()));
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
			var columns = new[] {"№ чека",
				"Дата",
				"ККМ",
				"Отдел",
				"Аннулирован",
				"Сумма розничная",
				"Сумма скидки",
				"Сумма с учетом скидки"};

			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("Экспорт");
			var row = 0;

			ExcelExporter.WriteRow(sheet, columns, row++);

			var rows = Items.Value.Select((o, i) => new object[] {
				o.Number,
				o.Date,
				o.KKM,
				o.Department,
				o.Cancelled,
				o.RetailSum,
				o.DiscontSum,
				o.Sum,
			});

			ExcelExporter.WriteRows(sheet, rows, row);

			return ExcelExporter.Export(book);
		}
	}
}

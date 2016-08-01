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
using System.ComponentModel;
using System.Reactive;


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
			AddressSelector = new AddressSelector(this);
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
		public AddressSelector AddressSelector { get; set; }
		public NotifyValue<IList<Selectable<string>>> KKMFilter { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			AddressSelector.Init();
			AddressSelector.FilterChanged.Cast<object>()
				.Merge(KKMFilter.SelectMany(x => x?.Select(c => c.Changed()).Merge().Throttle(Consts.FilterUpdateTimeout, UiScheduler)
					?? Observable.Empty<EventPattern<PropertyChangedEventArgs>>()))
				.Merge(KKMFilter.Where(x => x != null))
				.Subscribe(_ => Update(), CloseCancellation.Token);

			SampleData();
			//Update();
		}
		public void EnterItem()
		{
			if (CurrentItem.Value == null)
				return;
			main.ActiveItem = new CheckDetails(CurrentItem.Value);
		}

		protected override void OnDeactivate(bool close)
		{
			AddressSelector.OnDeactivate();
			base.OnDeactivate(close);
		}

		public override void Update()
		{
			if (KKMFilter.Value == null)
				return;
			var KKMs = KKMFilter.Value.Where(x => x.IsSelected).Select(x => x.Item).ToArray();
			var checks = Items.Value;
			var items = checks.Select(c => c).Where(c => KKMs.Contains(c.KKM)).ToList();
			Items.Value = items;
		}

		private IList<Check> TempFillItemsList()
		{
			var checks = new List<Check>();
			var check = new Check
			{
				Lines = new List<CheckLine>(),
				Id = 0,
				CheckType = CheckType.CheckReturn,
				Number = 100,
				Date = DateTime.Today.AddDays(-7),
				ChangeOpening = DateTime.Today.AddDays(-7),
				Status = Status.Open,
				Clerk = "Тестовый кассир",
				Department = Session.Query<Address>().First(),
				KKM = "1(0000000)",
				PaymentType = PaymentType.Cash,
				SaleType = SaleType.FullCost,
				Discont = 10,
				ChangeId = 0,
				ChangeNumber = 42,
				Cancelled = false,
			};
			check.Lines.Add(new CheckLine
			{
				Id = 0,
				Barcode = 124,
				ProductId = 10,
				ProducerId = 12,
				ProductName = "Тестовый продукт",
				RetailCost = 110,
				Cost = 100,
				Quantity = 1,
				DiscontSum = 10,
				CheckId = 0,
				ProductKind = 1,
			});
			check.CheckType = CheckType.CheckReturn;
			checks.Add(check);
			return checks;
		}

		private void SampleData()
		{
			Items.Value = TempFillItemsList();
			KKMFilter.Value.Add(new Selectable<string>("1(0000000)"));
		}

		public IEnumerable<IResult> PrintChecks()
		{
			return Preview("Чеки", new CheckDocument(Items.Value.ToArray()));
		}

		public IEnumerable<IResult> PrintReturnAct()
		{
			return Preview("Чеки", new ReturnActDocument(Items.Value.Where(x => x.CheckType == CheckType.CheckReturn).ToArray()));
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

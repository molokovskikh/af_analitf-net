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
	public class Checks : BaseScreen2
	{
		public Checks()
		{
			Begin.Value = DateTime.Today.AddDays(-7);
			End.Value = DateTime.Today;
			ChangeDate.Value = DateTime.Today;
			SearchBehavior = new SearchBehavior(this);
			KKMFilter = new NotifyValue<IList<Selectable<string>>>(new List<Selectable<string>>());
			AddressSelector = new AddressSelector(this);
			DisplayName = "Чеки";
			TrackDb(typeof(Check));
		}

		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		public NotifyValue<DateTime> ChangeDate { get; set; }
		public SearchBehavior SearchBehavior { get; set; }
		public NotifyValue<List<Check>> Items { get; set; }
		public NotifyValue<Check> CurrentItem { get; set; }
		public AddressSelector AddressSelector { get; set; }
		public NotifyValue<IList<Selectable<string>>> KKMFilter { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			AddressSelector.Init();
			AddressSelector.FilterChanged.Cast<object>()
				.Merge(DbReloadToken)
				.Merge(KKMFilter.SelectMany(x => x?.Select(c => c.Changed()).Merge()
					?? Observable.Empty<EventPattern<PropertyChangedEventArgs>>()))
				.Merge(KKMFilter.Where(x => x != null))
				.Merge(Begin.Select(x => (object)x))
				.Merge(End.Select(x => (object)x))
				.Throttle(Consts.FilterUpdateTimeout, UiScheduler)
				.SelectMany(_ => RxQuery(s => s.Query<Check>()
					.Where(c => c.Date <= End.Value.AddDays(1) && c.Date >= Begin.Value
						&& AddressSelector.GetActiveFilter().Contains(c.Department))
					.OrderByDescending(x => x.Date)
					.Fetch(x => x.Department)
					.ToList()))
				.Subscribe(Items);
		}

		public void EnterItem()
		{
			if (CurrentItem.Value == null)
				return;
			Shell.Navigate(new CheckDetails(CurrentItem.Value.Id));
		}

		protected override void OnDeactivate(bool close)
		{
			AddressSelector.OnDeactivate();
			base.OnDeactivate(close);
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
			Update();
			var columns = new[] {
				"№ чека",
				"Дата",
				"ККМ",
				"Отдел",
				"Аннулирован",
				"Сумма розничная",
				"Сумма скидки",
				"Сумма с учетом скидки"
			};

			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("Экспорт");
			var row = 0;

			ExcelExporter.WriteRow(sheet, columns, row++);

			var rows = Items.Value.Select((o, i) => new object[] {
				o.Id,
				o.Date,
				o.KKM,
				o.Department.Name,
				o.Cancelled,
				o.RetailSum,
				o.DiscountSum,
				o.Sum,
			});

			ExcelExporter.WriteRows(sheet, rows, row);

			return ExcelExporter.Export(book);
		}
	}
}

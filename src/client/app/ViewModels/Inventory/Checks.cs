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
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class ChecksStat : BaseNotify
	{
		private decimal _sum;
		private decimal _retailSum;

		public decimal Sum
		{
			get { return _sum; }
			set
			{
				if (_sum == value)
					return;
				_sum = value;
				OnPropertyChanged();
			}
		}

		public decimal RetailSum
		{
			get { return _retailSum; }
			set
			{
				if (_retailSum == value)
					return;
				_retailSum = value;
				OnPropertyChanged();
			}
		}
	}

	public class Checks : BaseScreen2, IPrintableStock
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

			PrintStockMenuItems = new ObservableCollection<MenuItem>();
			IsView = true;
			var stat = new ChecksStat();
			Stat = new List<ChecksStat> {
				stat
			};
			Items.Subscribe(x => {
				stat.Sum = x?.Sum(y => y.CheckType == CheckType.CheckReturn ? -y.Sum : y.Sum) ?? 0;
				stat.RetailSum = x?.Sum(y => y.CheckType == CheckType.CheckReturn ? -y.RetailSum : y.RetailSum) ?? 0;
			});
		}

		public List<ChecksStat> Stat { get; set; }
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
						&& AddressSelector.GetActiveFilter().Contains(c.Address))
					.OrderByDescending(x => x.Date)
					.Fetch(x => x.Address)
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
			return Preview("Акт возврата", new ReturnActDocument(Items.Value.Where(x => x.CheckType == CheckType.CheckReturn).ToArray()));
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
				o.Address.Name,
				o.Cancelled,
				o.RetailSum,
				o.DiscountSum,
				o.Sum,
			});

			ExcelExporter.WriteRows(sheet, rows, row);

			return ExcelExporter.Export(book);
		}

		public void SetMenuItems()
		{
			var item = new MenuItem {Header = "Чеки"};
			PrintStockMenuItems.Add(item);

			item = new MenuItem {Header = "Акт возврата"};
			PrintStockMenuItems.Add(item);

		}

		PrintResult IPrintableStock.PrintStock()
		{
			var docs = new List<BaseDocument>();
			if (!IsView) {
				foreach (var item in PrintStockMenuItems.Where(i => i.IsChecked)) {
					if ((string) item.Header == "Чеки")
						docs.Add(new CheckDocument(Items.Value.ToArray()));
					if ((string) item.Header == "Акт возврата")
						docs.Add(new ReturnActDocument(Items.Value.Where(x => x.CheckType == CheckType.CheckReturn).ToArray()));
				}
				return new PrintResult(DisplayName, docs, PrinterName);
			}

			if(String.IsNullOrEmpty(LastOperation) || LastOperation == "Чеки")
				Coroutine.BeginExecute(PrintChecks().GetEnumerator());
			if(LastOperation == "Акт возврата")
				Coroutine.BeginExecute(PrintReturnAct().GetEnumerator());
			return null;
		}

		public ObservableCollection<MenuItem> PrintStockMenuItems { get; set; }
		public string LastOperation { get; set; }
		public string PrinterName { get; set; }
		public bool IsView { get; set; }

		public bool CanPrintStock
		{
			get { return true; }
		}
	}
}

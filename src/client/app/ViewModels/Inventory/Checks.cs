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
			SetMenuItems();
			IsView = true;
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
			LastOperation = "Чеки";
			return Preview("Чеки", new CheckDocument(Items.Value.ToArray()));
		}

		public IEnumerable<IResult> PrintReturnAct()
		{
			LastOperation = "Акт возврата";
			return Preview("Акт возврата", new ReturnActDocument(Items.Value.Where(x => x.CheckType == CheckType.CheckReturn).ToArray()));
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

		private void SetMenuItems()
		{
			PrintStockMenuItems.Clear();
			var item = new MenuItem();
			item.Header = "Чеки";
			item.Click += (sender, args) => Coroutine.BeginExecute(PrintChecks().GetEnumerator());
			PrintStockMenuItems.Add(item);

			item = new MenuItem();
			item.Header = "Акт возврата";
			item.Click += (sender, args) => Coroutine.BeginExecute(PrintReturnAct().GetEnumerator());
			PrintStockMenuItems.Add(item);

			item = new MenuItem {Header = "Настройки"};
			item.Click += (sender, args) => Coroutine.BeginExecute(ReportSetting().GetEnumerator());
			PrintStockMenuItems.Add(item);

			foreach (var it in PrintStockMenuItems) {
				it.IsCheckable = false;
			}
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

		public IEnumerable<IResult> ReportSetting()
		{
			var req = new ReportSetting();
			yield return new DialogResult(req);
			PrinterName = req.PrinterName;
			if (req.IsView) {
				IsView = true;
				SetMenuItems();
			}

			if (req.IsPrint) {
				IsView = false;
				DisablePreview();
			}
		}

		public void DisablePreview()
		{
			foreach (var item in PrintStockMenuItems) {
				if (item.Header != "Настройки") {
					RemoveRoutedEventHandlers(item, MenuItem.ClickEvent);
					item.IsCheckable = true;
				}
			}
		}

		public static void RemoveRoutedEventHandlers(UIElement element, RoutedEvent routedEvent)
		{
			var eventHandlersStoreProperty = typeof (UIElement).GetProperty(
				"EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic);
			object eventHandlersStore = eventHandlersStoreProperty.GetValue(element, null);

			if (eventHandlersStore == null)
				return;

			var getRoutedEventHandlers = eventHandlersStore.GetType().GetMethod(
				"GetRoutedEventHandlers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			var routedEventHandlers = (RoutedEventHandlerInfo[]) getRoutedEventHandlers.Invoke(
				eventHandlersStore, new object[] {routedEvent});

			foreach (var routedEventHandler in routedEventHandlers)
				element.RemoveHandler(routedEvent, routedEventHandler.Handler);
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Parts;
using WindowManager = AnalitF.Net.Client.Config.Caliburn.WindowManager;
using Caliburn.Micro;
using Common.Tools;
using Common.Tools.Calendar;
using NHibernate.Linq;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System.Collections.Specialized;
using System.Reactive;

namespace AnalitF.Net.Client.ViewModels
{
	public enum RejectFilter
	{
		[Description("Все")] All,
		[Description("Измененные накладные")] Changed
	}

	public enum DocumentTypeFilter
	{
		[Description("Все")] All,
		[Description("Накладная")] Waybills,
		[Description("Отказ")] Rejects,
	}

	public class WaybillsViewModel : BaseScreen, IPrintable
	{
		public WaybillsViewModel()
		{
			DisplayName = "Документы";
			InitFields();
			SelectedWaybills = new List<Waybill>();

			Waybills.PropertyChanged += Waybills_PropertyChanged;
			WaybillsTotal = new ObservableCollection<WaybillTotal>();
			WaybillsTotal.Add(new WaybillTotal { TotalSum = 0.0m, TotalRetailSum = 0.0m });

			Begin = new NotifyValue<DateTime>(DateTime.Today.AddMonths(-3).FirstDayOfMonth());
			End = new NotifyValue<DateTime>(DateTime.Today);
			IsFilterByDocumentDate = new NotifyValue<bool>(true);
			CanDelete = CurrentWaybill.Select(v => v != null).ToValue();
			AddressSelector = new AddressSelector(this) {
				Description = "Все адреса"
			};

			Persist(IsFilterByDocumentDate, "IsFilterByDocumentDate");
			Persist(IsFilterByWriteTime, "IsFilterByWriteTime");
		}

    public void Waybills_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
	    if (Waybills.Value == null || WaybillsTotal.Count != 1) return;

	    WaybillsTotal.First().TotalSum = Waybills.Value.Sum(s => s.Sum);
	    WaybillsTotal.First().TotalRetailSum = Waybills.Value.Sum(s => s.RetailSum);
			WaybillsTotal.First().TotalDisplayedSum = Waybills.Value.Sum(s => s.DisplayedSum);
		}

		public NotifyValue<IList<Selectable<Supplier>>> Suppliers { get; set; }

		[Export]
		public NotifyValue<ObservableCollection<Waybill>> Waybills { get; set; }
		public ObservableCollection<WaybillTotal> WaybillsTotal { get; set; }
		public NotifyValue<Waybill> CurrentWaybill { get; set; }
		public List<Waybill> SelectedWaybills { get; set; }
		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		public NotifyValue<bool> IsFilterByDocumentDate { get; set; }
		public NotifyValue<bool> IsFilterByWriteTime { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<RejectFilter> RejectFilter { get; set; }
		public NotifyValue<DocumentTypeFilter> TypeFilter { get; set; }
		public AddressSelector AddressSelector { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			AddressSelector.Init();
			Env.RxQuery(s => s.Query<Supplier>().OrderBy(x => x.Name)
					.ToList()
					.Select(x => new Selectable<Supplier>(x)).ToList())
				.Subscribe(Suppliers);

			var supplierSelectionChanged = Suppliers.SelectMany(x => x?.Select(p => p.Changed()).Merge()
				.Throttle(Consts.FilterUpdateTimeout, UiScheduler)
				?? Observable.Empty<EventPattern<PropertyChangedEventArgs>>());

			Begin.Changed()
				.Merge(End.Changed())
				.Merge(IsFilterByDocumentDate.Changed())
				.Merge(RejectFilter.Changed())
				.Merge(supplierSelectionChanged)
				.Merge(AddressSelector.FilterChanged)
				.Merge(TypeFilter.Changed())
				.Merge(Suppliers.Where(x => x != null).Cast<object>())
				.Merge(Bus.Listen<Waybill>())
				.Throttle(TimeSpan.FromMilliseconds(50), Scheduler)
				.Subscribe(_ => Update(), CloseCancellation.Token);
		}

		protected override void OnDeactivate(bool close)
		{
			AddressSelector.OnDeactivate();
			base.OnDeactivate(close);
		}

		public void Delete()
		{
			if (!CanDelete)
				return;

			if (!Confirm("Удалить выбранные документы (накладные, отказы, документы)?"))
				return;

			foreach (var waybill in SelectedWaybills.ToArray()) {
				Waybills.Value.Remove(waybill);
				var item = waybill;
				Env.Query(s => {
					s.Delete(item);
					item.DeleteFiles(Settings.Value);
				}).LogResult();
			}
		}

		public IEnumerable<IResult> OpenFolders()
		{
			return Settings.Value.DocumentDirs.Select(dir => new OpenResult(dir));
		}

		public IResult AltExport()
		{
			var columns = new[] {"Дата",
				"Номер накладной",
				"Поставщик",
				"Сумма Опт без НДС",
				"Сумма Опт",
				"Сумма Розница",
				"Наценка,руб",
				"Наценка,%",
				"Сумма НДС",
				"Срок оплаты"};
			Func<Waybill, object[]> toRow = x => new object[] {
				x.DocumentDate.ToShortDateString(),
				x.ProviderDocumentId,
				x.SupplierName,
				x.SumWithoutTax,
				x.Sum,
				x.RetailSum,
				x.MarkupSum,
				x.Markup,
				x.TaxSum
			};
			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("Экспорт");
			var items = Waybills.Value;
			var groups = items.GroupBy(x => x.SafeSupplier);
			var row = 0;
			ExcelExporter.WriteRow(sheet, columns, row++);
			row += 2;
			foreach (var group in groups) {
				row = ExcelExporter.WriteRows(sheet, group.OrderByDescending(x => x.WriteTime).Select(toRow), row);
				row = WriteStatRow(sheet, row, @group, "Всего");
			}

			WriteStatRow(sheet, row, items, "Итого");

			return ExcelExporter.Export(book);
		}

		private static int WriteStatRow(ISheet sheet, int row, IEnumerable<Waybill> items, string label)
		{
			var statRow = sheet.CreateRow(row++);
			ExcelExporter.SetCellValue(statRow, 0, label);
			ExcelExporter.SetCellValue(statRow, 3, items.Sum(x => x.SumWithoutTax));
			var total = items.Sum(x => x.Sum);
			ExcelExporter.SetCellValue(statRow, 4, total);
			var retailTotal = items.Sum(x => x.RetailSum);
			ExcelExporter.SetCellValue(statRow, 5, retailTotal);
			ExcelExporter.SetCellValue(statRow, 6, retailTotal - total);
			if (total > 0)
				ExcelExporter.SetCellValue(statRow, 7, Math.Round((retailTotal / total - 1) * 100m, 2));
			ExcelExporter.SetCellValue(statRow, 8, items.Sum(x => x.TaxSum));
			row += 2;
			return row;
		}

		public void EnterWaybill()
		{
			var waybill = CurrentWaybill.Value;
			if (waybill == null)
				return;
			if (waybill.DocType.GetValueOrDefault(DocType.Waybill) == DocType.Reject)
				Shell.Navigate(new OrderRejectDetails(waybill.Id));
			else
				Shell.Navigate(new WaybillDetails(waybill.Id));
		}

		public void SearchLine()
		{
			Shell.Navigate(new WaybillLineSearch(Begin.Value, End.Value.AddDays(1)));
		}

		public override void Update()
		{
			if (Suppliers.Value == null)
				return;
			//уже не актуально но поучительно
			//скорбная песнь: при переходе на форму сводный заказ
			//wpf обновит состояние флага IsFilterByDocumentDate
			//когда эта форма уже закрыта
			//RadioButton имеет внутри статичный список всех кнопок на форме и обновляет их состояние
			//наверно в качестве родителя считается окно и для всех потомков с одинаковым GroupName
			//производится обновление

			Env.RxQuery(s => {
				var query = s.Query<Waybill>();
				var begin = Begin.Value;
				var end = End.Value.AddDays(1);
				if (IsFilterByDocumentDate) {
					query = query.Where(w => w.DocumentDate >= begin && w.DocumentDate <= end);
				}
				else {
					query = query.Where(w => w.WriteTime >= begin && w.WriteTime <= end);
				}

				if (RejectFilter.Value == ViewModels.RejectFilter.Changed) {
					query = query.Where(w => w.IsRejectChanged);
				}

				var supplierIds = Suppliers.Value.Where(x => x.IsSelected).Select(x => x.Item.Id).ToArray();
				if (supplierIds.Length != Suppliers.Value.Count)
					query = query.Where(w => supplierIds.Contains(w.Supplier.Id));

				var addressIds = AddressSelector.GetActiveFilter().Select(a => a.Id).ToArray();
				if (addressIds.Length != AddressSelector.Addresses.Count)
					query = query.Where(w => addressIds.Contains(w.Address.Id));

				if (TypeFilter.Value == DocumentTypeFilter.Waybills)
					query = query.Where(w => w.DocType == DocType.Waybill || w.DocType == null);
				else if (TypeFilter.Value == DocumentTypeFilter.Rejects)
					query = query.Where(w => w.DocType == DocType.Reject);

				var result = query
					.OrderByDescending(w => w.WriteTime)
					.Fetch(w => w.Supplier)
					.Fetch(w => w.Address)
					.ToObservableCollection();
				for(var i = 0; i < result.Count; i++)
					result[i].CalculateStyle(Address);
				return result;
			}).Subscribe(Waybills, CloseCancellation.Token);
		}

		public IEnumerable<IResult> Create()
		{
			if (Address == null)
				yield break;
			var waybill = new Waybill(Address);
			yield return new DialogResult(new CreateWaybill(waybill));
			Session.Save(waybill);
			Update();
		}

		public IEnumerable<IResult> DreamReport()
		{
			// выбор товаров
			var catalogList = new List<Catalog>();
			yield return new DialogResult(new CatalogViewModel(catalogList), fullScreen: true);

			// выбор остальных параметров
			var settings = new DreamReportSettings() {
				Begin = Begin.Value,
				End = End.Value,
				FilterByWriteTime = IsFilterByWriteTime.Value,
				CatalogIds = catalogList.Select(x => x.Id).ToArray(),
				CatalogNames = catalogList.Select(x => x.FullName).ToList().Implode()
			};
			yield return new DialogResult(new CreateDreamReport(settings));

			var commnand = new DreamReport(settings);
			yield return new Models.Results.TaskResult(commnand.ToTask(Shell.Config));
			yield return new OpenResult(commnand.Result);
		}

		public IEnumerable<IResult> VitallyImportantReport()
		{
			var commnand = new VitallyImportantReport {
				Begin = Begin.Value,
				End = End.Value,
				AddressIds = AddressSelector.GetActiveFilter().Select(x => x.Id).ToArray(),
				FilterByWriteTime = IsFilterByWriteTime.Value
			};
			yield return new Models.Results.TaskResult(commnand.ToTask(Shell.Config));
			yield return new OpenResult(commnand.Result);
		}

		public IEnumerable<IResult> RegulatorReport()
		{
			var commnand = new WaybillsReport(Shell.Config);
			yield return new Models.Results.TaskResult(commnand.ToTask(Shell.Config));
			yield return new OpenResult(commnand.Result);
		}

		public IEnumerable<IResult> WaybillMarkupReport()
		{
			var commnand = new WaybillMarkupReport();
			commnand.withNds = Manager.ShowMessageBox("Фактическую стоимость ЖНВЛП, в ценах производителя, за отчетный период (Столбец R) рассчитать с учетом НДС ?",
				"Отчет по розничным надбавкам к ценам на ЖВНЛП за год", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes;
			yield return new Models.Results.TaskResult(commnand.ToTask(Shell.Config));
			yield return new OpenResult(commnand.Result);
		}

		public bool CanPrint => true;

		public PrintResult Print()
		{
			return new PrintResult(DisplayName, new WaybillsDoc(Waybills.Value.ToArray()));
		}
	}
}
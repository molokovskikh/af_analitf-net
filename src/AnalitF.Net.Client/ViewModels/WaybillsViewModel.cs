using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Common.Tools;
using Common.Tools.Calendar;
using NHibernate.Linq;
using NPOI.HSSF.UserModel;

namespace AnalitF.Net.Client.ViewModels
{
	public enum RejectFilter
	{
		[Description("Все")] All,
		[Description("Измененные накладные")] Changed
	}

	public class WaybillsViewModel : BaseScreen
	{
		public WaybillsViewModel()
		{
			DisplayName = "Документы";
			SelectedWaybills = new List<Waybill>();
			Waybills = new NotifyValue<ObservableCollection<Waybill>>();
			CurrentWaybill = new NotifyValue<Waybill>();
			Begin = new NotifyValue<DateTime>(DateTime.Today.FirstDayOfMonth());
			End = new NotifyValue<DateTime>(DateTime.Today);
			IsFilterByDocumentDate = new NotifyValue<bool>(true);
			IsFilterByWriteTime = new NotifyValue<bool>();
			RejectFilter = new NotifyValue<RejectFilter>();
			CanDelete = new NotifyValue<bool>(() => CurrentWaybill.Value != null, CurrentWaybill);
		}

		public IList<Selectable<Supplier>> Suppliers { get; set; }

		[Export]
		public NotifyValue<ObservableCollection<Waybill>> Waybills { get; set; }
		public NotifyValue<Waybill> CurrentWaybill { get; set; }
		public List<Waybill> SelectedWaybills { get; set; }
		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		public NotifyValue<bool> IsFilterByDocumentDate { get; set; }
		public NotifyValue<bool> IsFilterByWriteTime { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<RejectFilter> RejectFilter { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Suppliers = StatelessSession.Query<Supplier>().OrderBy(s => s.Name)
				.ToList()
				.Select(i => new Selectable<Supplier>(i))
				.ToList();

			var supplierSelectionChanged = Suppliers
				.Select(a => Observable.FromEventPattern<PropertyChangedEventArgs>(a, "PropertyChanged"))
				.Merge()
				.Throttle(Consts.FilterUpdateTimeout, UiScheduler);

			var subscription = Begin.Changed()
				.Merge(End.Changed())
				.Merge(IsFilterByDocumentDate.Changed())
				.Merge(RejectFilter.Changed())
				.Merge(supplierSelectionChanged)
				.Subscribe(_ => Update());
			OnCloseDisposable.Add(subscription);
		}

		public void Delete()
		{
			if (!CanDelete)
				return;

			if (!Confirm("Удалить выбранные документы (накладные, отказы, документы)?"))
				return;

			foreach (var waybill in SelectedWaybills.ToArray()) {
				waybill.DeleteFiles(Settings.Value);
				Waybills.Value.Remove(waybill);
				StatelessSession.Delete(waybill);
			}
		}

		public IEnumerable<IResult> OpenFolders()
		{
			return Settings.Value.DocumentDirs.Select(dir => new OpenResult(dir));
		}

		public IResult AltExport()
		{
			var columns = new [] {"Дата",
				"Номер накладной",
				"Поставщик",
				"Сумма Опт без НДС",
				"Сумма Опт",
				"Сумма Розница",
				"Наценка,руб",
				"Наценка,%",
				"Сумма НДС",
				"Срок оплаты"};
			var items = Waybills.Value.Select(w => new object[] {
				w.DocumentDate,
				w.ProviderDocumentId,
				w.SupplierName,
				w.SumWithoutTax,
				w.Sum,
				w.RetailSum,
				w.MarkupSum,
				w.Markup,
				w.TaxSum
			});

			var book = ExcelExporter.ExportTable(columns, items);
			return excelExporter.Export(book);
		}

		public void EnterWaybill()
		{
			if (CurrentWaybill.Value == null)
				return;

			Shell.Navigate(new WaybillDetails(CurrentWaybill.Value.Id));
		}

		public void SearchLine()
		{
			Shell.Navigate(new WaybillLineSearch(Begin.Value, End.Value.AddDays(1)));
		}

		public override void Update()
		{
			//скорбная песнь: при переходе на форму сводный заказ
			//wpf обновит состояние флага IsFilterByDocumentDate
			//когда эта форма уже закрыта
			//RadioButton имеет внутри статичный список всех кнопок на форме и обновляет их состояние
			//наверно в качестве родителя считается окно и для всех потомков с одинаковым GroupName
			//производится обновление
			if (!StatelessSession.IsOpen)
				return;

			var query = StatelessSession.Query<Waybill>();

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

			var ids = Suppliers.Where(s => s.IsSelected).Select(s => s.Item.Id).ToArray();
			query = query.Where(w => ids.Contains(w.Supplier.Id));

			Waybills.Value = new ObservableCollection<Waybill>(query
				.OrderBy(w => w.WriteTime)
				.Fetch(w => w.Supplier)
				.ToList());
		}
	}
}
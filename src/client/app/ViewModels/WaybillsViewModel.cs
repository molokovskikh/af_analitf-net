using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using Common.Tools;
using Common.Tools.Calendar;
using NHibernate.Linq;

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


	public class WaybillsViewModel : BaseScreen
	{
		public WaybillsViewModel()
		{
			DisplayName = "Документы";
			SelectedWaybills = new List<Waybill>();
			Waybills = new NotifyValue<ObservableCollection<Waybill>>();
			CurrentWaybill = new NotifyValue<Waybill>();
			Begin = new NotifyValue<DateTime>(DateTime.Today.AddMonths(-3).FirstDayOfMonth());
			End = new NotifyValue<DateTime>(DateTime.Today);
			IsFilterByDocumentDate = new NotifyValue<bool>(true);
			IsFilterByWriteTime = new NotifyValue<bool>();
			RejectFilter = new NotifyValue<RejectFilter>();
			TypeFilter = new NotifyValue<DocumentTypeFilter>();
			CanDelete = CurrentWaybill.Select(v => v != null).ToValue();
			AddressSelector = new AddressSelector(this) {
				Description = "Все адреса"
			};
			Persist(IsFilterByDocumentDate, "IsFilterByDocumentDate");
			Persist(IsFilterByWriteTime, "IsFilterByWriteTime");
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
		public NotifyValue<DocumentTypeFilter> TypeFilter { get; set; }
		public AddressSelector AddressSelector { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			AddressSelector.Init();
			Suppliers = StatelessSession.Query<Supplier>().OrderBy(s => s.Name)
				.ToList()
				.Select(i => new Selectable<Supplier>(i))
				.ToList();

			var supplierSelectionChanged = Suppliers
				.Select(a => a.Changed())
				.Merge()
				.Throttle(Consts.FilterUpdateTimeout, UiScheduler);

			var subscription = Begin.Changed()
				.Merge(End.Changed())
				.Merge(IsFilterByDocumentDate.Changed())
				.Merge(RejectFilter.Changed())
				.Merge(supplierSelectionChanged)
				.Merge(AddressSelector.FilterChanged)
				.Merge(TypeFilter.Changed())
				.Subscribe(_ => Update());
			OnCloseDisposable.Add(subscription);
		}

		protected override void OnDeactivate(bool close)
		{
			AddressSelector.Deinit();
			base.OnDeactivate(close);
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
			return ExcelExporter.Export(book);
		}

		public void EnterWaybill()
		{
			if (CurrentWaybill.Value == null)
				return;

			if (CurrentWaybill.Value.DocType.GetValueOrDefault(DocType.Waybill) == DocType.Reject)
				Shell.Navigate(new OrderRejectDetails(CurrentWaybill.Value.Id));
			else
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

			var supplierIds = Suppliers.Where(s => s.IsSelected).Select(s => s.Item.Id).ToArray();
			if (supplierIds.Length != Suppliers.Count)
				query = query.Where(w => supplierIds.Contains(w.Supplier.Id));

			var addressIds = AddressSelector.GetActiveFilter().Select(a => a.Id).ToArray();
			if (addressIds.Length != AddressSelector.Addresses.Count)
				query = query.Where(w => addressIds.Contains(w.Address.Id));

			if (TypeFilter.Value == DocumentTypeFilter.Waybills)
				query = query.Where(w => w.DocType == DocType.Waybill || w.DocType == null);
			else if (TypeFilter.Value == DocumentTypeFilter.Rejects)
				query = query.Where(w => w.DocType == DocType.Reject);

			Waybills.Value = query
				.OrderByDescending(w => w.WriteTime)
				.Fetch(w => w.Supplier)
				.Fetch(w => w.Address)
				.ToObservableCollection();
			Waybills.Value.Each(w => w.CalculateStyle(Address));
		}

		public IEnumerable<IResult> Create()
		{
			if (Address == null)
				yield break;
			var waybill = new Waybill {
				Address = Address,
				WriteTime = DateTime.Now,
				DocumentDate = DateTime.Now,
				IsCreatedByUser = true
			};
			yield return new DialogResult(new CreateWaybill(waybill));
			Session.Save(waybill);
			Update();
		}

		public IEnumerable<IResult> VitallyImportantReport()
		{
			var commnand = new VitallyImportantReport {
				Begin = Begin.Value,
				End = End.Value,
				AddressIds = AddressSelector.GetActiveFilter().Select(x => x.Id).ToArray()
			};
			yield return new Models.Results.TaskResult(commnand.ToTask(Shell.Config));
			yield return new OpenResult(commnand.Result);
		}

		public IEnumerable<IResult> RegulatorReport()
		{
			var commnand = new WaybillsReport();
			yield return new Models.Results.TaskResult(commnand.ToTask(Shell.Config));
			yield return new OpenResult(commnand.Result);
		}
	}
}
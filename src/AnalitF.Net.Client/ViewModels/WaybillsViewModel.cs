﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Common.Tools.Calendar;
using NHibernate.Linq;
using NPOI.HSSF.UserModel;

namespace AnalitF.Net.Client.ViewModels
{
	public class WaybillsViewModel : BaseScreen
	{
		public WaybillsViewModel()
		{
			DisplayName = "Документы";
			Waybills = new NotifyValue<ObservableCollection<Waybill>>();
			CurrentWaybill = new NotifyValue<Waybill>();
			Begin = new NotifyValue<DateTime>(DateTime.Today.FirstDayOfMonth());
			End = new NotifyValue<DateTime>(DateTime.Today);
			IsFilterByDocumentDate = new NotifyValue<bool>(true);
			IsFilterByWriteTime = new NotifyValue<bool>();
			CanDelete = new NotifyValue<bool>(() => CurrentWaybill.Value != null, CurrentWaybill);

			Observable.Merge(
				Begin.Changes(),
				End.Changes(),
				IsFilterByDocumentDate.Changes())
				.Subscribe(_ => Update());
		}

		public IList<Supplier> Suppliers { get; set; }

		[Export]
		public NotifyValue<ObservableCollection<Waybill>> Waybills { get; set; }
		public NotifyValue<Waybill> CurrentWaybill { get; set; }
		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		public NotifyValue<bool> IsFilterByDocumentDate { get; set; }
		public NotifyValue<bool> IsFilterByWriteTime { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Suppliers = StatelessSession.Query<Supplier>().OrderBy(s => s.Name).ToList();

			Update();
		}

		public void Delete()
		{
			if (!CanDelete)
				return;

			if (!Confirm("Удалить выбранные документы (накладные, отказы, документы)?"))
				return;

			var waybill = CurrentWaybill.Value;
			Waybills.Value.Remove(waybill);
			StatelessSession.Delete(waybill);
		}

		public IEnumerable<IResult> OpenFolders()
		{
			return Settings.DocumentDirs.Select(dir => new OpenResult(dir));
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
				w.Supplier != null ? w.Supplier.FullName : "",
				w.SumWithoutTax,
				w.Sum,
				w.RetailSum,
				w.MarkupSum,
				w.Markup,
				w.TaxSum
			});

			var book = excelExporter.ExportTable(columns, items);
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

		private void Update()
		{
			var query = StatelessSession.Query<Waybill>();

			var begin = Begin.Value;
			var end = End.Value.AddDays(1);
			if (IsFilterByDocumentDate) {
				query = query.Where(w => w.DocumentDate >= begin && w.DocumentDate <= end);
			}
			else {
				query = query.Where(w => w.WriteTime >= begin && w.WriteTime <= end);
			}

			Waybills.Value = new ObservableCollection<Waybill>(query
				.OrderBy(w => w.WriteTime)
				.Fetch(w => w.Supplier)
				.ToList());
		}
	}
}
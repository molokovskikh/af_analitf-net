﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using Caliburn.Micro;
using NPOI.HSSF.UserModel;
using AnalitF.Net.Client.Models.Results;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class ReturnToSuppliers : BaseScreen2
	{
		public ReturnToSuppliers()
		{
			Begin.Value = DateTime.Today.AddDays(-30);
			End.Value = DateTime.Today;
			SelectedItems = new List<ReturnDoc>();
			CurrentItem.Subscribe(x => {
				CanEdit.Value = x != null;
				CanDelete.Value = x?.Status == DocStatus.NotPosted;
				CanPost.Value = x?.Status == DocStatus.NotPosted;
				CanUnPost.Value = x?.Status == DocStatus.Posted;
			});
			DisplayName = "Возврат поставщику";
			TrackDb(typeof(ReturnDoc));
		}

		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		[Export]
		public NotifyValue<List<ReturnDoc>> Items { get; set; }
		public NotifyValue<ReturnDoc> CurrentItem { get; set; }
		public List<ReturnDoc> SelectedItems { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanPost { get; set; }
		public NotifyValue<bool> CanUnPost { get; set; }
		public NotifyValue<bool> CanEdit { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			DbReloadToken
				.Merge(Begin.Select(x => (object)x))
				.Merge(End.Select(x => (object)x))
				.SelectMany(_ => RxQuery(s => s.Query<ReturnDoc>()
					.Where(x => x.Date > Begin.Value && x.Date < End.Value.AddDays(1))
					.Fetch(x => x.Address)
					.Fetch(x => x.Supplier)
					.OrderByDescending(x => x.Date).ToList()))
				.Subscribe(Items);
		}

		public IEnumerable<IResult> Create()
		{
			if (Address == null)
				yield break;
			var doc = new ReturnDoc(Address, User);
			yield return new DialogResult(new CreateReturnToSupplier(doc));
			Session.Save(doc);
			Update();
			Shell.Navigate(new ReturnToSupplierDetails(doc.Id));
		}

		public void Open()
		{
			if (!CanEdit)
				return;
			Shell.Navigate(new ReturnToSupplierDetails(CurrentItem.Value.Id));
		}

		public void Delete()
		{
			if (!Confirm("Удалить выбранный документ?"))
				return;
			var doc = Session.Load<ReturnDoc>(CurrentItem.Value.Id);
			doc.BeforeDelete();
			Session.Delete(doc);
			Session.Flush();
			Update();
		}

		public void Post()
		{
			if (!Confirm("Провести выбранный документ?"))
				return;
			var doc = Session.Load<ReturnDoc>(CurrentItem.Value.Id);
			if (!doc.Lines.Any()) {
				Manager.Warning("Пустой документ не может быть проведен");
				return;
			}
			doc.Post(Session);
			Session.Update(doc);
			Session.Flush();
			CurrentItem.Value.Status = doc.Status;
			CurrentItem.Refresh();
			Update();
		}

		public void UnPost()
		{
			if (!Confirm("Распровести выбранный документ?"))
				return;
			var doc = Session.Load<ReturnDoc>(CurrentItem.Value.Id);
			doc.UnPost(Session);
			Session.Update(doc);
			Session.Flush();
			CurrentItem.Value.Status = doc.Status;
			CurrentItem.Refresh();
			Update();
		}

		public void EnterItem()
		{
			Open();
		}

		public IResult ExportExcel()
		{
			Update();
			var columns = new[] {
				"Док. ИД",
				"Дата документа",
				"Отдел",
				"Поставщик",
				"Сумма закупки",
				"Сумма закупки с НДС",
				"Сумма розничная",
				"Число позиций",
				"Время закрытия",
				"Статус"
			};

			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("Экспорт");
			var row = 0;

			ExcelExporter.WriteRow(sheet, columns, row++);

			var rows = Items.Value.Select((o, i) => new object[] {
				o.Id,
				o.Date,
				o.AddressName,
				o.SupplierName,
				o.SupplierSumWithoutNds,
				o.SupplierSum,
				o.RetailSum,
				o.PosCount,
				o.CloseDate,
				o.StatusName,
			});

			ExcelExporter.WriteRows(sheet, rows, row);

			return ExcelExporter.Export(book);
		}
	}
}

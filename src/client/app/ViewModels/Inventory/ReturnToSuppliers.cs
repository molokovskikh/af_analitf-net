using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using Caliburn.Micro;
using NPOI.HSSF.UserModel;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class ReturnToSuppliers : BaseScreen2
	{
		public ReturnToSuppliers()
		{
			SelectedItems = new List<ReturnToSupplier>();
			CurrentItem.Subscribe(x => {
				CanEdit.Value = x != null;
				CanDelete.Value = x?.Status == DocStatus.Opened;
			});
			DisplayName = "Возврат поставщику";
			TrackDb(typeof(ReturnToSupplier));
		}

		[Export]
		public NotifyValue<List<ReturnToSupplier>> Items { get; set; }
		public NotifyValue<ReturnToSupplier> CurrentItem { get; set; }
		public List<ReturnToSupplier> SelectedItems { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanEdit { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			DbReloadToken
				.SelectMany(_ => RxQuery(s => s.Query<ReturnToSupplier>()
					.Fetch(x => x.Address)
					.Fetch(x => x.Supplier)
					.OrderByDescending(x => x.Date).ToList()))
				.Subscribe(Items);
		}

		public void Create()
		{
			Shell.Navigate(new ReturnToSupplierDetails(new ReturnToSupplier(Address)));
		}

		public void Edit()
		{
			if (!CanEdit)
				return;
			Shell.Navigate(new ReturnToSupplierDetails(CurrentItem.Value.Id));
		}

		public void Delete()
		{
			if (!Confirm("Удалить выбранные документы?"))
				return;

			StatelessSession.Delete(CurrentItem.Value);
			Update();
		}

		public void EnterItem()
		{
			Edit();
		}

		public IResult ExportExcel()
		{
			Update();
			var columns = new[] {
				"Док. ИД",
				//"Причина возврата",
				"Дата документа",
				"Отдел",
				"Поставщик",
				//"Партия №",
				//"Накладная №",
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
				//o.Причина возврата,
				o.Date,
				o.AddressName,
				o.SupplierName,
				//o.Партия №,
				//o.Накладная №,
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

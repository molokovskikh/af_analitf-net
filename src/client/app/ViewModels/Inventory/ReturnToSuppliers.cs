using System;
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

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class ReturnToSuppliers : BaseScreen2
	{
		public ReturnToSuppliers()
		{
			Begin.Value = DateTime.Today.AddDays(-7);
			End.Value = DateTime.Today;
			SelectedItems = new List<ReturnToSupplier>();
			CurrentItem.Subscribe(x => {
				CanEdit.Value = x != null;
				CanDelete.Value = x?.Status == DocStatus.NotPosted;
			});
			DisplayName = "Возврат поставщику";
			TrackDb(typeof(ReturnToSupplier));
		}

		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
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
				.Merge(Begin.Select(x => (object)x))
				.Merge(End.Select(x => (object)x))
				.SelectMany(_ => RxQuery(s => s.Query<ReturnToSupplier>()
					.Where(x => x.Date > Begin.Value && x.Date < End.Value.AddDays(1))
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

		public async Task Delete()
		{
			if (!Confirm("Удалить выбранный документ?"))
				return;

			CurrentItem.Value.BeforeDelete();
			await Env.Query(s => s.Delete(CurrentItem.Value));
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

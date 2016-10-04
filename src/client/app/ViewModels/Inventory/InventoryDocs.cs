using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using NHibernate;
using Caliburn.Micro;
using NPOI.HSSF.UserModel;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class InventoryDocs : BaseScreen2
	{
		public InventoryDocs()
		{
			IsAll = new NotifyValue<bool>(true);
			Begin.Value = DateTime.Today.AddDays(-7);
			End.Value = DateTime.Today;
			CurrentItem.Subscribe(x => {
				CanEdit.Value = x != null;
				CanDelete.Value = x != null;
			});
			DisplayName = "Излишки";
			TrackDb(typeof(InventoryDoc));
		}

		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		public NotifyValue<List<InventoryDoc>> Items { get; set; }
		public NotifyValue<InventoryDoc> CurrentItem { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanEdit { get; set; }
		public NotifyValue<bool> IsAll { get; set; }
		public NotifyValue<bool> IsOpened { get; set; }
		public NotifyValue<bool> IsClosed { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			DbReloadToken
				.Merge(Begin.Select(x => (object)x))
				.Merge(End.Select(x => (object)x))
				.Merge(IsOpened.Changed())
				.Merge(IsClosed.Changed())
				.SelectMany(_ => RxQuery(LoadItems))
				.Subscribe(Items);
		}

		public List<InventoryDoc> LoadItems(IStatelessSession session)
		{
			var query = session.Query<InventoryDoc>()
				.Where(x => x.Date > Begin.Value && x.Date < End.Value.AddDays(1));

			if (IsOpened)
				query = query.Where(x => x.Status == DocStatus.Opened);
			else if (IsClosed)
				query = query.Where(x => x.Status == DocStatus.Closed);

			var items = query.Fetch(x => x.Address)
				.OrderByDescending(x => x.Date)
				.ToList();
			return items;
		}

		public void Create()
		{
			Shell.Navigate(new EditInventoryDoc(new InventoryDoc(Address)));
		}

		public void Edit()
		{
			if (!CanEdit)
				return;
			Shell.Navigate(new EditInventoryDoc(CurrentItem.Value.Id));
		}

		public void Delete()
		{
			StatelessSession.Delete(CurrentItem.Value);
			Update();
		}

		public void EnterItem()
		{
			Edit();
		}

		public IResult ExportExcel()
		{
			var columns = new[] {"Номер",
				"Дата",
				"Адрес",
				"Сумма закупки",
				"Сумма закупки с НДС",
				"Сумма розничная",
				"Число позиций",
				"Время закрытия",
				"Статус",
			};

			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("Экспорт");
			var row = 0;

			ExcelExporter.WriteRow(sheet, columns, row++);

			var rows = Items.Value.Select((o, i) => new object[] {
				o.Id,
				o.Date,
				o.Address.Name,
				o.SupplySumWithoutNds,
				o.SupplySum,
				o.RetailSum,
				o.LinesCount,
				o.CloseDate,
				o.StatusName,
			});

			ExcelExporter.WriteRows(sheet, rows, row);
			return ExcelExporter.Export(book);
		}
	}
}

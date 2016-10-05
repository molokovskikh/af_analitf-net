using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using Caliburn.Micro;
using NHibernate;
using NPOI.HSSF.UserModel;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class DisplacementDocs : BaseScreen2
	{
		public DisplacementDocs()
		{
			SelectedItems = new List<DisplacementDoc>();
			CurrentItem.Subscribe(x => {
				CanEdit.Value = x != null;
				CanDelete.Value = x?.Status == DisplacementDocStatus.Opened;
			});
			DisplayName = "Внутренее перемещение";
			TrackDb(typeof(DisplacementDoc));
		}

		[Export]
		public NotifyValue<List<DisplacementDoc>> Items { get; set; }
		public NotifyValue<DisplacementDoc> CurrentItem { get; set; }
		public List<DisplacementDoc> SelectedItems { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanEdit { get; set; }

		public NotifyValue<bool> IsAll { get; set; }
		public NotifyValue<bool> IsOpened { get; set; }
		public NotifyValue<bool> IsClosed { get; set; }
		public NotifyValue<bool> IsEnd { get; set; }


		protected override void OnInitialize()
		{
			base.OnInitialize();
			DbReloadToken
				.Merge(IsOpened.Changed())
				.Merge(IsClosed.Changed())
				.Merge(IsEnd.Changed())
				.SelectMany(_ => RxQuery(LoadItems))
				.Subscribe(Items);
		}

		public List<DisplacementDoc> LoadItems(IStatelessSession session)
		{
			var query = session.Query<DisplacementDoc>();

			if (IsOpened)
				query = query.Where(x => x.Status == DisplacementDocStatus.Opened);
			else if (IsClosed)
				query = query.Where(x => x.Status == DisplacementDocStatus.Closed);
			else if (IsEnd)
				query = query.Where(x => x.Status == DisplacementDocStatus.End);

			var items = query.Fetch(x => x.Address)
					.Fetch(x => x.Recipient)
					.OrderByDescending(x => x.Date).ToList();

			return items;
		}

		public void Create()
		{
			Shell.Navigate(new EditDisplacementDoc(new DisplacementDoc(Address)));
		}

		public void Edit()
		{
			if (!CanEdit)
				return;
			Shell.Navigate(new EditDisplacementDoc(CurrentItem.Value.Id));
		}

		public void Delete()
		{
			if (!Confirm("Удалить выбранный документ?"))
				return;

			CurrentItem.Value.BeforeDelete();
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
				"Дата документа",
				"Отправитель",
				"Получатель",
				"Сумма закупки с НДС",
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
				o.SupplierSum,
				o.PosCount,
				o.CloseDate,
				o.StatusName,
			});

			ExcelExporter.WriteRows(sheet, rows, row);

			return ExcelExporter.Export(book);
		}
	}
}

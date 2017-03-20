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
using Common.NHibernate;
using NHibernate;
using NPOI.HSSF.UserModel;
using AnalitF.Net.Client.Models.Results;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class DisplacementDocs : BaseScreen2
	{
		public DisplacementDocs()
		{
			Begin.Value = DateTime.Today.AddDays(-30);
			End.Value = DateTime.Today;
			SelectedItems = new List<DisplacementDoc>();
			CurrentItem.Subscribe(x => {
				CanEdit.Value = x != null;
				CanPost.Value = x?.Status == DisplacementDocStatus.NotPosted;
				CanUnPost.Value = x?.Status == DisplacementDocStatus.Posted;
				CanDelete.Value = x?.Status == DisplacementDocStatus.NotPosted;
			});
			DisplayName = "Внутреннее перемещение";
			TrackDb(typeof(DisplacementDoc));
		}

		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		[Export]
		public NotifyValue<List<DisplacementDoc>> Items { get; set; }
		public NotifyValue<DisplacementDoc> CurrentItem { get; set; }
		public List<DisplacementDoc> SelectedItems { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanPost { get; set; }
		public NotifyValue<bool> CanUnPost { get; set; }
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
				.Merge(Begin.Changed())
				.Merge(End.Changed())
				.SelectMany(_ => RxQuery(LoadItems))
				.Subscribe(Items);
		}

		public List<DisplacementDoc> LoadItems(IStatelessSession session)
		{
			var query = session.Query<DisplacementDoc>();

			query = query.Where(x => x.Date > Begin.Value && x.Date < End.Value.AddDays(1));
			if (IsOpened)
				query = query.Where(x => x.Status == DisplacementDocStatus.NotPosted);
			else if (IsClosed)
				query = query.Where(x => x.Status == DisplacementDocStatus.Posted);
			else if (IsEnd)
				query = query.Where(x => x.Status == DisplacementDocStatus.End);

			var items = query.Fetch(x => x.Address)
					.Fetch(x => x.DstAddress)
					.OrderByDescending(x => x.Date).ToList();

			return items;
		}

		public IEnumerable<IResult> Create()
		{
			if (Address == null)
				yield break;
			var doc = new DisplacementDoc(Address, User);
			yield return new DialogResult(new CreateDisplacementDoc(doc));
			Session.Save(doc);
			Update();
			Shell.Navigate(new EditDisplacementDoc(doc.Id));
		}

		public void Open()
		{
			Edit();
		}

		public void Edit()
		{
			if (!CanEdit)
				return;
			Shell.Navigate(new EditDisplacementDoc(CurrentItem.Value.Id));
		}

		public async Task Delete()
		{
			if (!Confirm("Удалить выбранный документ?"))
				return;

			CurrentItem.Value.BeforeDelete();
			await Env.Query(s => s.Delete(CurrentItem.Value));
			Items.Value.Remove(CurrentItem.Value);
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

		public void Post()
		{
			if (!Confirm("Провести выбранный документ?"))
				return;
			var doc = Session.Load<DisplacementDoc>(CurrentItem.Value.Id);
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
			var doc = Session.Load<DisplacementDoc>(CurrentItem.Value.Id);
			doc.UnPost(Session);
			Session.Update(doc);
			Session.Flush();
			CurrentItem.Value.Status = doc.Status;
			CurrentItem.Refresh();
			Update();
		}
	}
}

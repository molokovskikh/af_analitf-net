using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using NHibernate;
using Caliburn.Micro;
using NPOI.HSSF.UserModel;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class InventoryDocs : BaseScreen2
	{
		private ReactiveCollection<InventoryDoc> items;

		public InventoryDocs()
		{
			Items = new ReactiveCollection<InventoryDoc>();
			IsAll = new NotifyValue<bool>(true);
			Begin.Value = DateTime.Today.AddDays(-30);
			End.Value = DateTime.Today;
			CurrentItem.Subscribe(x => {
				CanOpen.Value = x != null;
				CanDelete.Value = x != null && x.Status == DocStatus.NotPosted;
				CanPost.Value = x != null && x.Status == DocStatus.NotPosted;
				CanUnPost.Value = x != null && x.Status == DocStatus.Posted;
			});
			DisplayName = "Излишки";
			TrackDb(typeof(InventoryDoc));
		}

		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		public ReactiveCollection<InventoryDoc> Items
		{
			get { return items; }
			set
			{
				items = value;
				NotifyOfPropertyChange(nameof(Items));
			}
		}
		public NotifyValue<InventoryDoc> CurrentItem { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanOpen { get; set; }
		public NotifyValue<bool> CanPost { get; set; }
		public NotifyValue<bool> CanUnPost { get; set; }
		public NotifyValue<bool> IsAll { get; set; }
		public NotifyValue<bool> IsNotPosted { get; set; }
		public NotifyValue<bool> IsPosted { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			DbReloadToken
				.Merge(Begin.Select(x => (object)x))
				.Merge(End.Select(x => (object)x))
				.Merge(IsNotPosted.Changed())
				.Merge(IsPosted.Changed())
				.SelectMany(_ => RxQuery(LoadItems))
				.CatchSubscribe(BindItems, CloseCancellation);
		}

		public void BindItems(List<InventoryDoc> list)
		{
			Items = new ReactiveCollection<InventoryDoc>(list) {
				ChangeTrackingEnabled = true
			};
		}

		public List<InventoryDoc> LoadItems(IStatelessSession session)
		{
			var query = session.Query<InventoryDoc>()
				.Where(x => x.Date > Begin.Value && x.Date < End.Value.AddDays(1));

			if (IsNotPosted)
				query = query.Where(x => x.Status == DocStatus.NotPosted);
			else if (IsPosted)
				query = query.Where(x => x.Status == DocStatus.Posted);

			var items = query.Fetch(x => x.Address)
				.OrderByDescending(x => x.Date)
				.ToList();
			return items;
		}

		public void Create()
		{
			Shell.Navigate(new EditInventoryDoc(new InventoryDoc(Address)));
		}

		public void Open()
		{
			if (!CanOpen)
				return;
			Shell.Navigate(new EditInventoryDoc(CurrentItem.Value.Id));
		}

		public async Task Delete()
		{
			if (!Confirm("Удалить выбранный документ?"))
				return;
			await Env.Query(s => s.Delete(CurrentItem.Value));
			Update();
		}

		public void Post()
		{
			if (CurrentItem.Value == null)
				return;
			if (!Confirm("Провести выбранный документ?"))
				return;
			Session.Load<InventoryDoc>(CurrentItem.Value.Id).Post();
			Update();
		}

		public void UnPost()
		{
			if (CurrentItem.Value == null)
				return;
			if (!Confirm("Распровести выбранный документ?"))
				return;
			Session.Load<InventoryDoc>(CurrentItem.Value.Id).UnPost();
			Update();
		}

		public void EnterItem()
		{
			Open();
		}

		public IResult ExportExcel()
		{
			var columns = new[] {"Номер",
				"Дата",
				"Адрес",
				"Сумма розничная",
				"Число позиций",
				"Время закрытия",
				"Статус",
				"Комментарий",
			};

			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("Экспорт");
			var row = 0;

			ExcelExporter.WriteRow(sheet, columns, row++);

			var rows = Items.Select((o, i) => new object[] {
				o.Id,
				o.Date,
				o.Address.Name,
				o.RetailSum,
				o.LinesCount,
				o.CloseDate,
				o.StatusName,
				o.Comment,
			});

			ExcelExporter.WriteRows(sheet, rows, row);
			return ExcelExporter.Export(book);
		}
	}
}

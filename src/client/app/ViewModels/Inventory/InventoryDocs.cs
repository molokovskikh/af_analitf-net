using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
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
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;

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
				CanPrint.Value = x != null;
				CanTags.Value = x != null;
				CanDelete.Value = x?.Status == DocStatus.NotPosted;
				CanPost.Value = x?.Status == DocStatus.NotPosted;
				CanUnPost.Value = x?.Status == DocStatus.Posted;
			});
			DisplayName = "Излишки";
			TrackDb(typeof(InventoryDoc));
		}

		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		[Export]
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
		public NotifyValue<bool> CanPrint { get; set; }
		public NotifyValue<bool> CanTags { get; set; }
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
				.Subscribe(_ => Update(), CloseCancellation.Token);
		}

		public override void Update()
		{
			Session.Clear();
			var query = Session.Query<InventoryDoc>()
				.Where(x => x.Date > Begin.Value && x.Date < End.Value.AddDays(1));

			if (IsNotPosted)
				query = query.Where(x => x.Status == DocStatus.NotPosted);
			else if (IsPosted)
				query = query.Where(x => x.Status == DocStatus.Posted);

			var items = query.Fetch(x => x.Address)
				.OrderByDescending(x => x.Date)
				.ToList();

			Items = new ReactiveCollection<InventoryDoc>(items)
			{
				ChangeTrackingEnabled = true
			};
		}

		public IEnumerable<IResult> Create()
		{
			if (Address == null)
				yield break;
			var doc = new InventoryDoc(Address, User);
			yield return new DialogResult(new CreateInventoryDoc(doc));
			Session.Save(doc);
			Update();
			Shell.Navigate(new EditInventoryDoc(doc.Id));
		}

		public void Open()
		{
			if (!CanOpen)
				return;
			Shell.Navigate(new EditInventoryDoc(CurrentItem.Value.Id));
		}

		public async Task Delete()
		{
			if (!CanDelete)
				return;
			if (!Confirm("Удалить выбранный документ?"))
				return;
			CurrentItem.Value.BeforeDelete(Session);
			await Env.Query(s => {
				foreach (var line in CurrentItem.Value.Lines) {
					// если сток создавался вместе со строкой и пустой - можно удалить
					if (line.StockIsNew && line.Stock.Quantity == 0 && line.Stock.ReservedQuantity == 0)
						s.Delete(line.Stock);
				}
				s.Delete(CurrentItem.Value);
			});
			Update();
		}

		public void Post()
		{
			if (!Confirm("Провести выбранный документ?"))
				return;
			var doc = Session.Load<InventoryDoc>(CurrentItem.Value.Id);
			if (!doc.Lines.Any()) {
				Manager.Warning("Пустой документ не может быть проведен");
				return;
			}
			doc.Post();
			Session.Update(doc);
			Session.Flush();
			CurrentItem.Value.Status = doc.Status;
			CurrentItem.Refresh();
			Update();
			Bus.SendMessage(nameof(InventoryDoc), "db");
			Bus.SendMessage(nameof(Stock), "db");
		}

		public void UnPost()
		{
			if (!Confirm("Распровести выбранный документ?"))
				return;
			var doc = Session.Load<InventoryDoc>(CurrentItem.Value.Id);
			doc.UnPost();
			Session.Update(doc);
			Session.Flush();
			CurrentItem.Value.Status = doc.Status;
			CurrentItem.Refresh();
			Update();
			Bus.SendMessage(nameof(InventoryDoc), "db");
			Bus.SendMessage(nameof(Stock), "db");
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

		public IEnumerable<IResult> Print()
		{
			return Preview("Излишки", new InventoryDocument(CurrentItem.Value.Lines.ToArray()));
		}

		public void Tags()
		{
			var tags = CurrentItem.Value.Lines.Select(x => x.Stock.GeTagPrintable(User?.FullName)).ToList();
			Shell.Navigate(new Tags(null, tags));
		}
	}
}

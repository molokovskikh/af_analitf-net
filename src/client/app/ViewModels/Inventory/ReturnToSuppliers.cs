using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using NPOI.HSSF.UserModel;
using System.ComponentModel;
using System.Reactive;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class ReturnToSuppliers : BaseScreen2
	{
		public ReturnToSuppliers()
		{
			DisplayName = "Возврат поставщику";
			SelectedItems = new List<ReturnToSupplier>();
			CanDelete = CurrentItem.Select(v => v != null).ToValue();
		}

		[Export]
		public NotifyValue<ObservableCollection<ReturnToSupplier>> Items { get; set; }
		public NotifyValue<ReturnToSupplier> CurrentItem { get; set; }
		public List<ReturnToSupplier> SelectedItems { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
		}

		public void EnterItem()
		{
			if (CurrentItem.Value == null)
				return;
			Shell.Navigate(new ReturnToSupplierDetails(CurrentItem.Value.Id));
		}

		protected override void OnDeactivate(bool close)
		{
			base.OnDeactivate(close);
		}

		public void Delete()
		{
			if (!CanDelete)
				return;

			if (!Confirm("Удалить выбранные документы?"))
				return;

			foreach (var item in SelectedItems.ToArray())
			{
				Items.Value.Remove(item);
				StatelessSession.Delete(item);
				Items.Refresh();
			}
		}

		public IEnumerable<IResult> Create()
		{
			if (Address == null)
				yield break;
			var returnToSupplier = new ReturnToSupplier(Address);
			yield return new DialogResult(new CreateReturnToSupplier(returnToSupplier));
			Session.Save(returnToSupplier);
			Update();
		}

		public override void Update()
		{
			RxQuery(x => {
				return x.Query<ReturnToSupplier>()
				.OrderByDescending(y => y.DateDoc)
				.Fetch(y => y.Supplier)
				.Fetch(y => y.Department)
				.ToList()
				.ToObservableCollection();
			})
			.Subscribe(Items);
		}

		public IResult ExportExcel()
		{
			Update();
			var columns = new[] {
				"Док. ИД",
				"Док. №",
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
				o.NumDoc,
				//o.Причина возврата,
				o.DateDoc,
				o.DepartmentName,
				o.SupplierName,
				//o.Партия №,
				//o.Накладная №,
				o.SupplierSumWithoutNds,
				o.SupplierSum,
				o.RetailSum,
				o.PosCount,
				o.DateClosing,
				o.StatusName,
			});

			ExcelExporter.WriteRows(sheet, rows, row);

			return ExcelExporter.Export(book);
		}
	}
}

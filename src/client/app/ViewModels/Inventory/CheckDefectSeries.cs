using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using Common.Tools;
using NPOI.SS.UserModel;
using System.Collections.ObjectModel;
using NPOI.HSSF.UserModel;
using System;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class CheckDefectSeries : BaseScreen2
	{
		public NotifyValue<List<Stock>> Items { get; set; }
		public NotifyValue<Stock> CurrentItem { get; set; }
		public NotifyValue<bool> IsAll { get; set; }
		public NotifyValue<bool> IsRejected { get; set; }
		public NotifyValue<bool> IsForceRejected { get; set; }
		public NotifyValue<DateTime?> Begin { get; set; }
		public NotifyValue<DateTime?> End { get; set; }

		private string Name;

		public CheckDefectSeries()
		{
			CurrentItem = new NotifyValue<Stock>();
			Name = User?.FullName ?? "";

			IsAll = new NotifyValue<bool>(true);
			IsRejected = new NotifyValue<bool>();
			IsForceRejected = new NotifyValue<bool>();
			Begin = new NotifyValue<DateTime?>();
			End = new NotifyValue<DateTime?>();
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			RxQuery(x => x.Query<Stock>().OrderBy(y => y.Product).ToList())
				.Subscribe(Items);

			var subscription = Begin.Changed()
				.Merge(End.Changed())
				.Merge(IsRejected.Changed())
				.Merge(IsForceRejected.Changed())
				.Subscribe(_ => Update());
			OnCloseDisposable.Add(subscription);
		}

		public IEnumerable<IResult> EnterItems()
		{
			var stock = CurrentItem.Value;
			if (stock != null)
				yield return new DialogResult(new EditDefectSeries(stock.Id));

			Session.Refresh(stock);
			Update();
		}

		public IEnumerable<IResult> DisplayItem()
		{
			var stock = CurrentItem.Value;
			if (stock != null)
				yield return new DialogResult(new EditStock(stock.Id));

			Session.Refresh(stock);
			Update();
		}

		public override void Update()
		{
			var query = StatelessSession.Query<Stock>();

			if (IsRejected)
				query = query.Where(x => x.RejectStatus == RejectStatus.Rejected);
			else if (IsForceRejected)
				query = query.Where(x => x.RejectStatus == RejectStatus.ForceRejected);

			if (Begin.HasValue && Begin.Value.HasValue) {
				var rejectUpLetters = StatelessSession.Query<Reject>().Where(x => x.LetterDate >= Begin.Value.Value).Select(x => x.Id).ToList();
				query = query.Where(x => x.RejectId.HasValue && rejectUpLetters.Contains(x.RejectId.Value));
			}
			if (End.HasValue && End.Value.HasValue)
			{
				var end = End.Value.Value.AddDays(1);
				var rejectDownLetters = StatelessSession.Query<Reject>().Where(x => x.LetterDate <= end).Select(x => x.Id).ToList();
				query = query.Where(x => x.RejectId.HasValue && rejectDownLetters.Contains(x.RejectId.Value));
			}

			Items.Value = query.OrderBy(y => y.Product).ToList();
		}

		public IResult ExportExcel()
		{
			var columns = new[] {"Штрих-код",
				"Товар",
				"Производитель",
				"Серия",
				"Кол-во",
				"Брак"};

			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("Экспорт");
			var row = 0;

			ExcelExporter.WriteRow(sheet, columns, row++);

			var rows = Items.Value.Select((o, i) => new object[] {
				o.Barcode,
				o.Product,
				o.Producer,
				o.Seria,
				o.Count,
				o.RejectStatusName
			});

			ExcelExporter.WriteRows(sheet, rows, row);

			return ExcelExporter.Export(book);
		}

		public IEnumerable<IResult> PrintDefectStock()
		{
			return Preview("Товарные запасы", new DefectStockDocument(Items.Value.ToArray()));
		}

		private IEnumerable<IResult> Preview(string name, BaseDocument doc)
		{
			var docSettings = doc.Settings;
			if (docSettings != null)
			{
				yield return new DialogResult(new SimpleSettings(docSettings));
			}
			yield return new DialogResult(new PrintPreviewViewModel(new PrintResult(name, doc)), fullScreen: true);
		}

	}
}
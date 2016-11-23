﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using NHibernate.Linq;
using NPOI.HSSF.UserModel;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class CheckDetails : BaseScreen2, IPrintableStock
	{
		private uint id;

		public CheckDetails()
		{
			DisplayName = "Чек";
			Lines = new NotifyValue<IList<CheckLine>>(new List<CheckLine>());
			SetMenuItems();
		}

		public CheckDetails(uint id)
			: this()
		{
			this.id = id;
		}

		public NotifyValue<Check> Header { get; set; }
		public NotifyValue<CheckLine> CurrentLine { get; set; }
		public NotifyValue<IList<CheckLine>> Lines { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			RxQuery(s => s.Query<Check>()
					.Fetch(x => x.Department)
					.FirstOrDefault(y => y.Id == id))
				.Subscribe(Header);
			RxQuery(x => {
					return x.Query<CheckLine>().Where(y => y.CheckId == id)
						.ToList()
						.ToObservableCollection();
				})
				.Subscribe(Lines);
		}

		public IEnumerable<IResult> PrintCheckDetails()
		{
			LastOperation = "Чеки";
			return Preview("Чеки", new CheckDetailsDocument(Lines.Value.ToArray(), Header.Value));
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

		public IResult ExportExcel()
		{
			var columns = new[] {
				"№",
				"Штрих-код",
				"Название товара",
				"Количество",
				"Цена розничная",
				"Сумма розничная",
				"Сумма скидки",
				"Сумма с учетом скидки"
			};

			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("Экспорт");
			var row = 0;

			ExcelExporter.WriteRow(sheet, columns, row++);

			var rows = Lines.Value.Select((o, i) => new object[] {
				o.Id,
				o.Barcode,
				o.Product,
				o.Quantity,
				o.RetailCost,
				o.RetailSum,
				o.DiscontSum,
				o.Sum,
			});

			ExcelExporter.WriteRows(sheet, rows, row);

			return ExcelExporter.Export(book);
		}

		private void SetMenuItems()
		{
			PrintStockMenuItems = new ObservableCollection<MenuItem>();
			var item = new MenuItem();
			item.Header = "Чеки";
			item.Click += (sender, args) => Coroutine.BeginExecute(PrintCheckDetails().GetEnumerator());
			PrintStockMenuItems.Add(item);
		}

		void IPrintableStock.PrintStock()
		{
			if(String.IsNullOrEmpty(LastOperation) || LastOperation == "Чеки")
				Coroutine.BeginExecute(PrintCheckDetails().GetEnumerator());
		}

		public ObservableCollection<MenuItem> PrintStockMenuItems { get; set; }
		public string LastOperation { get; set; }
		public bool CanPrintStock
		{
			get { return true; }
		}
	}
}

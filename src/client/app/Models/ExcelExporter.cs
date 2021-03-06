﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.Tools;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace AnalitF.Net.Client.Models
{
	public interface IExportable
	{
		NotifyValue<bool> CanExport { get; }

		IResult Export();
	}

	public class ExportAttribute : Attribute
	{
	}

	public class ExcelExporter
	{
		private Screen model;

		public string ExportDir;
		public string[] Properties;

		public ExcelExporter(Screen model, string[] properties, string dir)
		{
			this.model = model;
			this.Properties = properties;
			ExportDir = dir;
			ActiveProperty = new NotifyValue<string>();
			CanExport = new NotifyValue<bool>();
			ActiveProperty.Select(p => Properties.Length > 0 && (p == null || Properties.Contains(p))).Subscribe(CanExport);
		}

		public NotifyValue<bool> CanExport { get; set; }
		public NotifyValue<string> ActiveProperty { get; set; }

		public IResult Export()
		{
			var grid = FindGrid();
			if (grid == null)
				return null;

			var items = grid.ItemsSource;
			if (items == null)
				return null;

			var columns = grid.Columns.OfType<DataGridBoundColumn>()
				.OrderBy(c => c.DisplayIndex)
				.Where(c => c.Visibility == Visibility.Visible)
				.ToArray();
			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("Экспорт");
			var rowIndex = 0;
			var row = sheet.CreateRow(rowIndex++);
			for(var i = 0; i < columns.Length; i++) {
				row.CreateCell(i).SetCellValue(DataGridHelper.GetHeader(columns[i]));
			}
			foreach (var item in items) {
				row = sheet.CreateRow(rowIndex++);
				for(var i = 0; i < columns.Length; i++) {
					var path = ((Binding)columns[i].Binding).Path.Path;
					SetCellValue(row, i, Util.GetValue(item, path));
				}
			}

			return Export(book);
		}

		public IResult Export(HSSFWorkbook book)
		{
			var filename = Path.Combine(ExportDir, Path.ChangeExtension(Path.GetRandomFileName(), "xls"));
			using (var file = File.Create(filename)) {
				book.Write(file);
			}

			return new OpenResult(filename);
		}

		private DataGrid FindGrid()
		{
			var view = (UserControl) model.GetView();
			if (view == null)
				return null;
			return view.Descendants<DataGrid>()
				.Where(g => Properties.Contains(g.Name))
				.OrderByDescending(g => Convert.ToUInt32(g.IsKeyboardFocusWithin) * 1000
					+ Convert.ToUInt32(ActiveProperty.Value == g.Name) * 1000
					+ Convert.ToUInt32(g.IsVisible) * 10)
				.FirstOrDefault();
		}

		public static void SetCellValue(IRow row, int i, object value)
		{
			value = NullableHelper.GetNullableValue(value);
			if (value == null)
				return;

			if (value is bool) {
				var cell = row.CreateCell(i);
				if ((bool)value)
					cell.SetCellValue("Да");
				else
					cell.SetCellValue("Нет");
			}
			else if (value is DateTime) {
				var cell = row.CreateCell(i);
				cell.SetCellValue(value.ToString());
			}
			else if (Util.IsNumeric(value)) {
				var cell = row.CreateCell(i, CellType.Numeric);
				cell.SetCellValue(Convert.ToDouble(value));
			}
			else {
				var cell = row.CreateCell(i);
				cell.SetCellValue(value.ToString());
			}
		}

		public static HSSFWorkbook ExportTable(string[] columns, IEnumerable<object[]> items, int startRow = 0)
		{
			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("Экспорт");
			var rowIndex = startRow;
			var row = sheet.CreateRow(rowIndex++);

			for(var i = 0; i < columns.Length; i++) {
				row.CreateCell(i).SetCellValue(columns[i]);
			}
			WriteRows(sheet, items, rowIndex);

			return book;
		}

		public static IRow WriteRow(ISheet sheet, object[] items, int rowIndex)
		{
			var row = sheet.CreateRow(rowIndex++);
			for (var i = 0; i < items.Length; i++) {
				SetCellValue(row, i, items[i]);
			}
			return row;
		}

		public static int WriteRows(ISheet sheet, IEnumerable<object[]> items, int rowIndex)
		{
			foreach (var item in items) {
				var row = sheet.CreateRow(rowIndex++);
				for (var i = 0; i < item.Length; i++) {
					SetCellValue(row, i, item[i]);
				}
			}
			return rowIndex;
		}
	}
}
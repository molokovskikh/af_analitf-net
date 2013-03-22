using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using NPOI.HSSF.UserModel;

namespace AnalitF.Net.Client.Models
{
	public interface IExportable
	{
		bool CanExport { get; }

		IResult Export();
	}

	public class ExportAttribute : Attribute
	{
	}

	public class ExcelExporter
	{
		private List<PropertyInfo> properties;
		private Screen model;

		public ExcelExporter(Screen model)
		{
			this.model = model;
			properties = model.GetType().GetProperties()
				.Where(p => p.GetCustomAttributes(typeof(ExportAttribute), true).Length > 0)
				.ToList();
		}

		public bool CanExport
		{
			get { return properties != null; }
		}

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
			var filename = Path.ChangeExtension(Path.GetRandomFileName(), "xls");
			using(var file = File.OpenWrite(filename)) {
				var book = new HSSFWorkbook();
				var sheet = book.CreateSheet("Экспорт");
				var rowIndex = 0;
				var row = sheet.CreateRow(rowIndex++);
				for(var i = 0; i < columns.Length; i++) {
					row.CreateCell(i).SetCellValue(columns[i].Header.ToString());
				}
				foreach (var item in items) {
					row = sheet.CreateRow(rowIndex++);
					for(var i = 0; i < columns.Length; i++) {
						row.CreateCell(i).SetCellValue(GetValue(columns[i], item));
					}
				}
				book.Write(file);
			}

			return new OpenResult(filename);
		}

		private DataGrid FindGrid()
		{
			if (properties == null)
				return null;

			var names = properties.Select(p => p.Name).ToArray();
			var view = (UserControl) model.GetView();
			if (view == null)
				return null;
			return view.DeepChildren().OfType<DataGrid>().Where(g => names.Contains(g.Name))
				.OrderByDescending(g => Convert.ToUInt32(g.IsKeyboardFocusWithin) * 100 + Convert.ToUInt32(g.IsVisible) * 10)
				.FirstOrDefault();
		}

		private string GetValue(DataGridBoundColumn column, object offer)
		{
			var path = ((Binding)column.Binding).Path.Path;
			var parts = path.Split('.');

			var value = offer;
			foreach (var part in parts) {
				if (value == null)
					return "";
				var type = value.GetType();
				var property = type.GetProperty(part);
				if (property == null)
					return "";
				value = property.GetValue(value, null);
			}
			if (value == null)
				return "";
			if (value is bool) {
				if (((bool)value)) {
					return "Да";
				}
				else {
					return "Нет";
				}
			}
			return value.ToString();
		}
	}
}
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;

namespace AnalitF.Net.Client.Helpers
{
	public class DataGridHelper
	{
		public static DependencyProperty ColumnDisplayNameProperty
			= DependencyProperty.RegisterAttached("ColumnDisplayName", typeof(String), typeof(DataGridHelper));

		public static string GetColumnDisplayName(DependencyObject src)
		{
			return src.GetValue(ColumnDisplayNameProperty) as string;
		}

		public static void SetColumnDisplayName(DependencyObject src, string value)
		{
			src.SetValue(ColumnDisplayNameProperty, value);
		}

		public static void Centrify(DataGrid grid)
		{
			if (grid == null)
				return;
			var scrollViewer = grid.Descendants<ScrollViewer>().FirstOrDefault();
			if (scrollViewer == null)
				return;
			var selected = grid.SelectedItem;
			if (selected == null)
				return;
			var container = grid.ItemContainerGenerator.ContainerFromItem(selected);
			if (container == null)
				return;
			var index = grid.ItemContainerGenerator.IndexFromContainer(container);
			var offset = index - scrollViewer.ViewportHeight / 2;
			if (offset < 0)
				return;
			scrollViewer.ScrollToVerticalOffset(offset);
		}

		//магия, когда datagrid получает фокус он не ставит фокус на ячейку
		//и ввод обрабатывается KeyboardNavigation который стрелки интерпретирует
		//как перевод фокуса нужно ставить фокус не на таблицу
		//а на ячейку что бы фокус работал как следует
		public static bool Focus(DataGrid grid)
		{
			if (grid.SelectedItem == null)
				grid.SelectedItem = grid.Items.Cast<object>().FirstOrDefault();
			var item = grid.SelectedItem;
			if (item == null) {
				var scroll = grid.Descendants<ScrollViewer>().FirstOrDefault(v => v.Name == "DG_ScrollViewer");
				if (scroll != null) {
					return scroll.Focus();
				}
				return false;
			}

			grid.ScrollIntoView(item);
			var container = (DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(item);
			var column = grid.CurrentCell.Column
				?? grid.Columns.FirstOrDefault(c => c.Visibility == Visibility.Visible);
			var cell = GetCell(container, column, grid.Columns);
			if (cell != null)
				return cell.Focus();
			return false;
		}

		public static DataGridCell GetCell(DataGridRow rowContainer, DataGridColumn column, ObservableCollection<DataGridColumn> columns)
		{
			if (rowContainer == null)
				return null;
			var presenter = rowContainer.VisualChild<DataGridCellsPresenter>();
			if (presenter == null)
				return null;
			for(var i = 0; i < columns.Count; i++) {
				var cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(i);
				if (cell != null && (column == null || cell.Column == column)) {
					return cell;
				}
			}
			return null;
		}

		public static DataGridCell GetCell(DataGrid grid, DataGridCellInfo info)
		{
			var container = (DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(info.Item);
			if (container == null)
				return null;
			var cell = GetCell(container, info.Column, grid.Columns);
			return cell;
		}

		public static void CalculateColumnWidth(DataGrid grid, string template, string header)
		{
			var column = FindColumn(grid.Columns, header);
			if (column == null)
				return;

			column.Width = CalculateWidth(grid, template);
		}

		public static DataGridLength CalculateWidth(DataGrid grid, string template)
		{
			var text = new FormattedText(template,
				CultureInfo.CurrentUICulture,
				grid.FlowDirection,
				new Typeface(grid.FontFamily, grid.FontStyle, grid.FontWeight, grid.FontStretch),
				grid.FontSize,
				grid.Foreground);
			 return new DataGridLength(text.Width + 4 /*отступы*/+ 2 /*окаймление*/, DataGridLengthUnitType.Pixel);
		}

		public static DataGridColumn FindColumn(DataGrid grid, string name)
		{
			return FindColumn(grid.Columns, name);
		}

		public static DataGridColumn FindColumn(ObservableCollection<DataGridColumn> columns, string name)
		{
			return columns.FirstOrDefault(c => String.Equals(GetHeader(c), name, StringComparison.CurrentCultureIgnoreCase));
		}

		public static string GetHeader(DataGridColumn column)
		{
			return column.Header as string
				?? (column.Header as TextBlock)?.Text
				?? GetColumnDisplayName(column);
		}

		public static void CalculateColumnWidths(DataGrid grid)
		{
			int index = grid.Columns.OfType<DataGridBoundColumn>().IndexOf(c => ((Binding)c.Binding).Path.Path == "Note");
			if (index >= 0) {
				grid.Columns.Insert(
					index + 1,
					new DataGridTextColumnEx {
						Width = new DataGridLength(13, DataGridLengthUnitType.Star),
						Header = "Штрихкод",
						Binding = new Binding("BarCode"),
						Visibility = Visibility.Collapsed
					});
				CalculateColumnWidth(grid, "0000000000000", "Штрихкод");
			}
			var producerColumn = FindColumn(grid, "Кат.производитель");
			if (producerColumn != null) {
				var propertiesColumn = FindColumn(grid, "Кат.свойства");
				if (propertiesColumn == null) {
					propertiesColumn = new DataGridTextColumnEx {
						Header = "Кат.свойства",
						Binding = new Binding("Properties"),
						Visibility = Visibility.Collapsed,
						Width = CalculateWidth(grid, "Кат.свойства"),
					};
					grid.Columns.Insert(grid.Columns.IndexOf(producerColumn) + 1, propertiesColumn);
				}
				else {
					propertiesColumn.Width = CalculateWidth(grid, "Кат.свойства");
				}
			}
			var col = FindColumn(grid, "Срок годн.");
			if (col != null) {
				col.SortMemberPath = "Exp";
			}
			CalculateColumnWidth(grid, "00.00", "Наценка поставщика");
			CalculateColumnWidth(grid, "000.00", "Цена производителя");
			CalculateColumnWidth(grid, "000.00", "Пред.зарег.цена");
			CalculateColumnWidth(grid, "0000.00", "Цена поставщика");
			CalculateColumnWidth(grid, "0000.00", "Эффективность");

			grid.Loaded += (sender, args) => {
				var dataGrid = (DataGrid)sender;
				var screen = dataGrid.DataContext as BaseScreen;
				var removeSupplierCost = screen == null
					|| screen.User == null
					|| !screen.User.IsDelayOfPaymentEnabled
					|| !screen.User.ShowSupplierCost;
				if (removeSupplierCost) {
					var column = FindColumn(dataGrid.Columns, "Цена поставщика");
					if (column != null)
						dataGrid.Columns.Remove(column);
				}
			};
		}
	}
}
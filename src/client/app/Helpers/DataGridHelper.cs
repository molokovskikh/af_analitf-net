using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using AnalitF.Net.Client.ViewModels;
using NHibernate.Engine;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Helpers
{
	public class DataGridHelper
	{
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
		//как перевод фокуса нужно ставить фокус не на грид
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

		public static void CalculateColumnWidth(DataGrid dataGrid, string template, string header)
		{
			var column = FindColumn(dataGrid.Columns, header);
			if (column == null)
				return;

			var text = new FormattedText(template,
				CultureInfo.CurrentUICulture,
				dataGrid.FlowDirection,
				new Typeface(dataGrid.FontFamily, dataGrid.FontStyle, dataGrid.FontWeight, dataGrid.FontStretch),
				dataGrid.FontSize,
				dataGrid.Foreground);
			column.Width = new DataGridLength(text.Width + 4/*отступы*/ + 2/*окаймление*/, DataGridLengthUnitType.Pixel);
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
			if (column.Header is string)
				return column.Header.ToString();
			if (column.Header is TextBlock)
				return ((TextBlock)column.Header).Text;
			return "";
		}

		public static void CalculateColumnWidths(DataGrid grid)
		{
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
					|| !screen.User.IsDeplayOfPaymentEnabled
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
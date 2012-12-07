using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Common.Tools;
using DataGrid = AnalitF.Net.Client.Controls.DataGrid;

namespace AnalitF.Net.Client.Helpers
{
	public static class XamlExtentions
	{
		public static IEnumerable<DependencyObject> Children(this Visual visual)
		{
			var count = VisualTreeHelper.GetChildrenCount(visual);
			for (var i = 0; i < count; i++) {
				yield return VisualTreeHelper.GetChild(visual, i);
			}
		}

		public static T GetVisualChild<T>(this Visual parent) where T : Visual
		{
			T child = default(T);

			int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < numVisuals; i++)
			{
				Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
				child = v as T;
				if (child == null)
				{
					child = GetVisualChild<T>(v);
				}
				if (child != null)
				{
					break;
				}
			}

			return child;
		}

		//магия, когда datagrid получает фокус он не ставит фокус на ячейку
		//и ввод обрабатывается KeyboardNavigation который стрелки интерпретирует
		//как перевод фокуса нужно ставить фокус не на грид
		//а на ячейку что бы фокус работал как следует
		public static void Focus(DataGrid grid)
		{
			if (grid.SelectedItem == null)
				grid.SelectedItem = grid.Items.Cast<object>().FirstOrDefault();
			var item = grid.SelectedItem;

			if (item == null)
				return;
			grid.ScrollIntoView(item);
			var container = grid.ItemContainerGenerator.ContainerFromItem(item);
			var column = grid.CurrentCell.Column;
			var columnIndex = 0;
			if (column != null)
				columnIndex = column.DisplayIndex;
			var cell = GetCell((DataGridRow)container, columnIndex);
			if (cell != null)
				Keyboard.Focus(cell);
		}

		public static DataGridCell GetCell(DataGridRow rowContainer, int column)
		{
			if (rowContainer == null)
				return null;
			var presenter = GetVisualChild<DataGridCellsPresenter>(rowContainer);
			if (presenter == null)
				return null;
			return (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
		}

		public static bool IsVisual(this DependencyObject o)
		{
			return o is Visual || o is Visual3D;
		}

		public static IEnumerable<DependencyObject> Children(this DependencyObject o)
		{
			var visualCount = IsVisual(o) ? VisualTreeHelper.GetChildrenCount(o) : 0;
			if (visualCount > 0) {
				for (int i = 0; i < visualCount; i++) {
					yield return VisualTreeHelper.GetChild(o, i);
				}
			}
			else {
				var contentControl = o as ContentControl;
				if (contentControl != null) {
					var content = contentControl.Content as DependencyObject;
					if (content != null)
						yield return content;
				}

				foreach (var child in LogicalTreeHelper.GetChildren(o).OfType<DependencyObject>()) {
					yield return child;
				}
			}
		}

		public static IEnumerable<object> Parents(DependencyObject o)
		{
			var parent = Parent(o);
			while (parent != null) {
				yield return parent;
				parent = Parent(parent);
			}
		}

		public static DependencyObject Parent(this DependencyObject o)
		{
			var frameworkElement = o as FrameworkElement;
			if (frameworkElement != null)
				return frameworkElement.Parent;
			return VisualTreeHelper.GetParent(o);
		}

		public static IEnumerable<DependencyObject> DeepChildren(this DependencyObject view)
		{
			return view.Children().Flat(d => Children(d));
		}
	}
}
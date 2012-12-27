using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace AnalitF.Net.Client.Helpers
{
	public class DataGridHelper
	{
		public static void Centrify(DataGrid grid)
		{
			var scrollViewer = grid.DeepChildren().OfType<ScrollViewer>().FirstOrDefault();
			if (scrollViewer == null)
				return;
			var currentItem = grid.CurrentItem;
			if (currentItem == null)
				return;
			var container = grid.ItemContainerGenerator.ContainerFromItem(currentItem);
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
		public static void Focus(DataGrid grid)
		{
			if (grid.SelectedItem == null)
				grid.SelectedItem = grid.Items.Cast<object>().FirstOrDefault();
			var item = grid.SelectedItem;

			if (item == null)
				return;
			grid.ScrollIntoView(item);
			var container = grid.ItemContainerGenerator.ContainerFromItem(item);
			var cell = GetCell((DataGridRow)container, grid.CurrentCell.Column);
			if (cell != null)
				Keyboard.Focus(cell);
		}

		public static DataGridCell GetCell(DataGridRow rowContainer, DataGridColumn column)
		{
			if (rowContainer == null)
				return null;
			var columnIndex = 0;
			if (column != null)
				columnIndex = column.DisplayIndex;
			var presenter = rowContainer.GetVisualChild<DataGridCellsPresenter>();
			if (presenter == null)
				return null;
			return (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex);
		}
	}
}
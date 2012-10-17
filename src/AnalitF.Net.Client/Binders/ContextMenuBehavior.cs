using System.Windows.Controls;
using System.Windows.Data;
using AnalitF.Net.Client.Extentions;
using DataGrid = AnalitF.Net.Client.Controls.DataGrid;

namespace AnalitF.Net.Client.Binders
{
	public class ContextMenuBehavior
	{
		public static void Attach(DataGrid grid)
		{
			var contextMenu = new ContextMenu();
			foreach (var column in grid.Columns) {
				var menuItem = new MenuItem {
					Header = column.Header,
					IsCheckable = true
				};
				var binding = new Binding("Visibility") {
					Converter = new VisibilityToBoolConverter(),
					Source = column
				};
				menuItem.SetBinding(MenuItem.IsCheckedProperty, binding);
				contextMenu.Items.Add(menuItem);
			}
			grid.ContextMenu = contextMenu;
		}
	}
}
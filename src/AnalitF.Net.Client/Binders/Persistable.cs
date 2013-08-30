using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AnalitF.Net.Client.Extentions;
using DataGrid = AnalitF.Net.Client.Controls.DataGrid;

namespace AnalitF.Net.Client.Binders
{
	public class Persistable
	{
		public static readonly DependencyProperty PersistColumnSettingsProperty =
			DependencyProperty.RegisterAttached("PersistColumnSettings",
				typeof(bool),
				typeof(Persistable),
				new PropertyMetadata(false, PropertyChangedCallback));

		public static bool GetPersistColumnSettings(DependencyObject o)
		{
			return (bool)o.GetValue(PersistColumnSettingsProperty);
		}

		public static void SetPersistColumnSettings(DependencyObject o, bool value)
		{
			o.SetValue(PersistColumnSettingsProperty, value);
		}

		private static void PropertyChangedCallback(DependencyObject o, DependencyPropertyChangedEventArgs a)
		{
			var grid = (DataGrid)o;
			if ((bool)a.NewValue) {
				Attach(grid);
			}
			else {
				Detach(grid);
			}
		}

		private static void Detach(DataGrid grid)
		{
			grid.ContextMenu = null;
		}

		private static void Attach(DataGrid grid)
		{
			if (!grid.IsInitialized) {
				EventHandler loaded = null;
				loaded = (sender, args) => {
					var dataGrid = (DataGrid)sender;
					dataGrid.Initialized -= loaded;
					Attach(dataGrid);
				};
				grid.Initialized += loaded;
				return;
			}

			var contextMenu = new ContextMenu();
			contextMenu.Items.Add(new MenuItem {
				Header = "Восстановить значения по умолчанию",
				Command = Commands.InvokeViewModel,
				CommandParameter = new { Method = "ResetView", grid },
			});
			contextMenu.Items.Add(new Separator());
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
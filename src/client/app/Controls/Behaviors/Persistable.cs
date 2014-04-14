using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interactivity;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Extentions;

namespace AnalitF.Net.Client.Controls.Behaviors
{
	public class Persistable : Behavior<DataGrid>
	{
		public static readonly DependencyProperty DescriptionProperty
			= DependencyProperty.RegisterAttached("Description",
				typeof(string), typeof(Persistable), new PropertyMetadata(default(string)));

		protected override void OnAttached()
		{
			var contextMenu = new ContextMenu();
			contextMenu.Items.Add(new MenuItem {
				Header = "Восстановить значения по умолчанию",
				Command = Commands.InvokeViewModel,
				CommandParameter = new { Method = "ResetView", grid = AssociatedObject },
			});
			contextMenu.Items.Add(new Separator());
			foreach (var column in AssociatedObject.Columns) {
				var header = GetDescription(column) ?? Helpers.DataGridHelper.GetHeader(column);
				var menuItem = new MenuItem {
					Header = header,
					IsCheckable = true
				};
				var binding = new Binding("Visibility") {
					Converter = new BoolToCollapsedConverter(),
					Source = column
				};
				menuItem.SetBinding(MenuItem.IsCheckedProperty, binding);
				contextMenu.Items.Add(menuItem);
			}
			AssociatedObject.ContextMenu = contextMenu;
		}

		protected override void OnDetaching()
		{
			AssociatedObject.ContextMenu = null;
		}

		public static string GetDescription(DependencyObject element)
		{
			return (string)element.GetValue(DescriptionProperty);
		}

		public static void SetDescription(DependencyObject element, string value)
		{
			element.SetValue(DescriptionProperty, value);
		}
	}
}
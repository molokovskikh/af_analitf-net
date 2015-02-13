using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interactivity;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.ViewModels;

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
			var item = new MenuItem {
				Header = "Столбцы...",
			};
			item.Click += (sender, args) => {
				var screen = AssociatedObject.DataContext as BaseScreen;
				if (screen != null)
					ViewModelHelper.ProcessResult(screen.ConfigureGrid(AssociatedObject));
			};
			var copy = new MenuItem {
				Header = "Копировать"
			};
			BindingOperations.SetBinding(copy, MenuItem.IsEnabledProperty, new Binding("ClipboardCopyMode") {
				Source = AssociatedObject,
				BindsDirectlyToSource = true,
				Converter = new LambdaConverter<DataGridClipboardCopyMode>(m => m != DataGridClipboardCopyMode.None)
			});
			copy.Click += (sender, args) => CopyToClipboard(AssociatedObject);
			contextMenu.Items.Add(item);
			contextMenu.Items.Add(copy);
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

		public static void CopyToClipboard(DataGrid grid)
		{
			var columns = grid.Columns.Where(c => c.Visibility == Visibility.Visible).OrderBy(c => c.DisplayIndex).ToArray();
			var writer = new StringWriter();
			for (var i = 0; i < columns.Length; i++) {
				writer.Write(Helpers.DataGridHelper.GetHeader(columns[i]));
				if (i == columns.Length - 1)
					writer.WriteLine();
				else
					writer.Write('\t');
			}
			foreach (var item in grid.Items) {
				for (var i = 0; i < columns.Length; i++) {
					var value = columns[i].OnCopyingCellClipboardContent(item);
					writer.Write(value);
					if (i == columns.Length - 1)
						writer.WriteLine();
					else
						writer.Write('\t');
				}
			}
			Clipboard.SetText(writer.ToString());
		}
	}
}
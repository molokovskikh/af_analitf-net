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
			contextMenu.Items.Add(item);
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
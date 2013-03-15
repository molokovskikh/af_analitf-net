using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace AnalitF.Net.Client.Controls
{
	public class ContextMenuOnClick
	{
		public static DependencyProperty ShowContextMenuProperty
			= DependencyProperty.RegisterAttached("ShowContextMenu",
				typeof(bool),
				typeof(ContextMenuOnClick),
				new FrameworkPropertyMetadata(false, ShowContextMenuChanged));

		public static DependencyProperty SelectedItemProperty
			= DependencyProperty.RegisterAttached("SelectedItem",
				typeof(object),
				typeof(ContextMenuOnClick),
				new FrameworkPropertyMetadata(null));

		public static DependencyProperty ItemsProperty
			= DependencyProperty.RegisterAttached("Items",
				typeof(IList),
				typeof(ContextMenuOnClick),
				new FrameworkPropertyMetadata(null, ItemsChanged));

		public static DependencyProperty IsCanChoseProperty
			= DependencyProperty.RegisterAttached("IsCanChose",
				typeof(bool),
				typeof(ContextMenuOnClick));

		private static void ItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var items = GetItems(d);
			SetIsCanChose(d, items != null && items.Count > 1);
		}

		private static void ShowContextMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var button = (Button)d;
			d.CoerceValue(ItemsProperty);
			button.Click += (sender, args) => {
				if (args.OriginalSource != button)
					return;

				var items = GetItems(button);

				if (items == null || items.Count < 2) {
					SetSelectedItem(button, items == null ? null : items.Cast<object>().FirstOrDefault());
					return;
				}

				args.Handled = true;
				var menu = new ContextMenu();
				foreach (var item in items) {
					var menuItem = new MenuItem {
						Header = ((dynamic)item).Name.ToString(),
						DataContext = item
					};
					menuItem.Click += (o, eventArgs) => {
						SetSelectedItem(button, ((FrameworkElement)o).DataContext);
						button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, o));
					};
					menu.Items.Add(menuItem);
				}
				menu.PlacementTarget = button;
				menu.Placement = PlacementMode.Right;
				menu.IsOpen = true;
			};
		}

		public static IList GetItems(DependencyObject d)
		{
			return (IList)d.GetValue(ItemsProperty);
		}

		public static void SetItems(DependencyObject d, object value)
		{
			d.SetValue(ItemsProperty, value);
		}

		public static bool GetIsCanChose(DependencyObject d)
		{
			return (bool)d.GetValue(IsCanChoseProperty);
		}

		public static void SetIsCanChose(DependencyObject d, object value)
		{
			d.SetValue(IsCanChoseProperty, value);
		}

		public static object GetSelectedItem(DependencyObject o)
		{
			return o.GetValue(SelectedItemProperty);
		}

		public static void SetSelectedItem(DependencyObject o, object value)
		{
			o.SetValue(SelectedItemProperty, value);
		}

		public static bool GetShowContextMenu(DependencyObject o)
		{
			return (bool)o.GetValue(ShowContextMenuProperty);
		}

		public static void SetShowContextMenu(DependencyObject o, bool value)
		{
			o.SetValue(ShowContextMenuProperty, value);
		}
	}
}
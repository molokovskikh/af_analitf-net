using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Binders
{
	public class FocusBehavior
	{
		public static DependencyProperty DefaultFocusProperty
			= DependencyProperty.RegisterAttached("DefaultFocus", typeof(IInputElement), typeof(FocusBehavior));

		public static void Bind(object viewModel, DependencyObject view, object context)
		{
			if (viewModel is IConductor)
				return;

			var element = view as FrameworkElement;
			if (element == null)
				return;

			IInputElement lastFocusedElement = null;
			element.Loaded += (sender, args) => {
				if (lastFocusedElement != null) {
					if (lastFocusedElement is DataGridCell) {
						var grid = ((DataGridCell)lastFocusedElement).VisualParents().OfType<DataGrid>().FirstOrDefault();
						if (grid != null)
							DataGridHelper.Focus(grid);
					}
					else
						Keyboard.Focus(lastFocusedElement);
				}
				else {
					var defaultFocus = GetDefaultFocus(view) ?? FromContent(view);
					if (defaultFocus == null)
						return;
					if (defaultFocus is DataGrid) {
						DataGridHelper.Focus((DataGrid)defaultFocus);
					}
					else
						Keyboard.Focus(defaultFocus);
				}
			};
			element.Unloaded += (sender, args) => {
				if (element.IsKeyboardFocusWithin)
					lastFocusedElement = Keyboard.FocusedElement;
			};
		}

		//если мы показываем диалог то DefaultFocus будет у UserControl
		private static IInputElement FromContent(DependencyObject view)
		{
			var window = view as Window;
			if (window == null)
				return null;
				var content = window.Content as DependencyObject;
			if (content == null)
				return null;
			return GetDefaultFocus(content);
		}

		public static IInputElement GetDefaultFocus(DependencyObject d)
		{
			return (IInputElement)d.GetValue(DefaultFocusProperty);
		}

		public static void SetDefaultFocus(DependencyObject d, IInputElement value)
		{
			d.SetValue(DefaultFocusProperty, value);
		}

	}
}
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Binders
{
	public class FocusBehavior
	{
		public static DependencyProperty GridProperty
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
					Keyboard.Focus(lastFocusedElement);
				}
				else {
					var defaultFocus = GetDefaultFocus(view);
					if (defaultFocus == null)
						return;
					if (defaultFocus is DataGrid)
						DataGridHelper.Focus((DataGrid)defaultFocus);
					else
						Keyboard.Focus(defaultFocus);
				}
			};
			element.Unloaded += (sender, args) => {
				if (element.IsKeyboardFocusWithin)
					lastFocusedElement = Keyboard.FocusedElement;
			};
		}

		public static IInputElement GetDefaultFocus(DependencyObject d)
		{
			return (IInputElement)d.GetValue(GridProperty);
		}

		public static void SetDefaultFocus(DependencyObject d, IInputElement value)
		{
			d.SetValue(GridProperty, value);
		}

	}
}
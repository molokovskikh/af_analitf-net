using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Threading;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;
using Action = System.Action;

namespace AnalitF.Net.Client.Controls.Behaviors
{
	public class Focusable : Behavior<Control>
	{
		private IInputElement lastFocusedElement;

		public static DependencyProperty DefaultFocusProperty
			= DependencyProperty.RegisterAttached("DefaultFocus", typeof(IInputElement), typeof(Focusable));

		protected override void OnAttached()
		{
			AssociatedObject.Loaded += Loaded;
			AssociatedObject.Unloaded += Unloaded;
		}

		protected override void OnDetaching()
		{
			AssociatedObject.Loaded -= Loaded;
			AssociatedObject.Unloaded -= Unloaded;
		}

		private void Unloaded(object sender, RoutedEventArgs e)
		{
			if (AssociatedObject.IsKeyboardFocusWithin)
				lastFocusedElement = Keyboard.FocusedElement;
		}

		private void Loaded(object sender, RoutedEventArgs args)
		{
			if (lastFocusedElement != null) {
				if (lastFocusedElement is DataGridCell) {
					var grid = ((DataGridCell)lastFocusedElement).VisualParents()
						.OfType<DataGrid>()
						.FirstOrDefault();
					if (grid != null)
						DataGridHelper.Focus(grid);
				}
				else
					Keyboard.Focus(lastFocusedElement);
			}
			else {
				var defaultFocus = GetDefaultFocus(AssociatedObject) ?? FromContent(AssociatedObject);
				if (defaultFocus == null)
					return;
				if (defaultFocus is DataGrid) {
					//иногда visual tree data grid оказывается не построенным хотя он и говорит что
					//все загружено, если фокус не удалось установить всего скорее visual tree не создан
					//нужно повторить операцию после того как все будет загружено
					if (!DataGridHelper.Focus((DataGrid)defaultFocus)) {
						Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => {
							DataGridHelper.Focus((DataGrid)defaultFocus);
						}));
					}
				}
				else {
					Keyboard.Focus(defaultFocus);
				}
			}
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
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
			if (AssociatedObject.IsKeyboardFocusWithin) {
				lastFocusedElement = Keyboard.FocusedElement;
				//если фокус установлен на ячейку то сохранять его нет смысла
				//тк ячейка может быть разрешена и тогда мы не сможем восстановить фокус
				//ячейка будет разрушена например если после будет вызвана следующая цепочка событий
				//форма 1 -> форма 2 (открытие) -> форма 1 (возврат) -> форма 1 (загрузка данных после из-за изменений на форме 2)
				if (lastFocusedElement is DataGridCell) {
					lastFocusedElement = ((DataGridCell)lastFocusedElement).VisualParents()
						.OfType<DataGrid>()
						.FirstOrDefault();
				}
			}
		}

		private void Loaded(object sender, RoutedEventArgs args)
		{
			if (lastFocusedElement != null) {
				if (lastFocusedElement is DataGrid)
					DataGridHelper.Focus((DataGrid)lastFocusedElement);
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
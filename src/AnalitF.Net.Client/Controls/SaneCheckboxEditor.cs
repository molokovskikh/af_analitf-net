using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AnalitF.Net.Client.Controls
{
	//исправляет поведение редактора для колонок с checkbox
	//из коробки что бы изменить состояние checkbox нужно два клика
	//один что бы установить фокус
	//второй что бы изменить состояние
	//для этого мы подписываемя на событие и проверяем состояние ячейки
	//и входим в режим редактирования
	public class SaneCheckboxEditor
	{
		private static PropertyInfo DataGridOwner;

		public static void Register()
		{
			var type = typeof(DataGridCell);
			//наш обработчик должен быть зарегистрирован после обработчика в DataGridCell
			RuntimeHelpers.RunClassConstructor(type.TypeHandle);
			EventManager.RegisterClassHandler(type, UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler(MouseDown), true);
			DataGridOwner = type.GetProperty("DataGridOwner", BindingFlags.NonPublic | BindingFlags.Instance);
		}

		private static void MouseDown(object sender, MouseButtonEventArgs e)
		{
			var cell = (DataGridCell)sender;
			var focusWithin = cell.IsKeyboardFocusWithin;
			var isCtrlKeyPressed = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
			var isCheckBox = cell.Column is DataGridCheckBoxColumn;
			if (isCheckBox
				&& e.Handled// событие уже должно быть обработано в DataGridCell, там будет установлен фокус
				&& focusWithin
				&& !isCtrlKeyPressed
				&& !cell.IsEditing
				&& !cell.IsReadOnly
				&& cell.IsSelected) {
				var dataGridOwner = (System.Windows.Controls.DataGrid)DataGridOwner.GetValue(cell, null);
				if (dataGridOwner != null)  {
					dataGridOwner.BeginEdit(e);
				}
			}
		}
	}
}
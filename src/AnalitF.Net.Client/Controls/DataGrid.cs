using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Controls
{
	public class DataGrid : System.Windows.Controls.DataGrid
	{
		public static DependencyProperty ShowAddressColumnProperty
			= DependencyProperty.RegisterAttached("ShowAddressColumn",
				typeof(bool),
				typeof(DataGrid),
				new FrameworkPropertyMetadata(false, ShowAddressColumnPropertyChanged));

		//свойство нужно тк в 4.0
		//биндинг на CurrentItem сбрасывается http://connect.microsoft.com/visualstudio/feedback/details/696155/wpf
		public static DependencyProperty CurrentItemStubProperty
			= DependencyProperty.RegisterAttached("CurrentItemStub",
				typeof(object),
				typeof(DataGrid),
				new FrameworkPropertyMetadata(null, CurrentItemStubChanged));

		protected static void CurrentItemStubChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var grid = (DataGrid)d;
			grid.CurrentItem = e.NewValue;
		}

		public object CurrentItemStub
		{
			get { return GetValue(CurrentItemStubProperty); }
			set { SetValue(CurrentItemStubProperty, value); }
		}

		//это хак, тк дата биндинг не работает для DataGridColumn
		//это фактически его эмуляция
		private static void ShowAddressColumnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var grid = (DataGrid)d;
			var column = grid.Columns.FirstOrDefault(c => c.Header.Equals("Адрес заказа"));
			if (column == null)
				return;

			column.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
		}

		public bool ShowAddressColumn
		{
			get { return (bool)GetValue(ShowAddressColumnProperty); }
			set { SetValue(ShowAddressColumnProperty, value); }
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Enter) {
				if (IsReadOnly)
					return;
				var cell = DataGridHelper.GetCell(this, CurrentCell);
				if (cell == null)
					return;
				if (!cell.IsEditing)
					return;
			}

			base.OnKeyDown(e);
		}

		protected override void OnExecutedDelete(ExecutedRoutedEventArgs e)
		{
			if (CanUserDeleteRows)
				base.OnExecutedDelete(e);
		}

		protected override void OnCanExecuteDelete(CanExecuteRoutedEventArgs e)
		{
			if (CanUserDeleteRows) {
				base.OnCanExecuteDelete(e);
			}
			else {
				e.Handled = false;
				e.ContinueRouting = true;
			}
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			var viewer = this.Descendants<ScrollViewer>().FirstOrDefault(s => s.Name == "DG_ScrollViewer");
			if (viewer != null) {
				viewer.Focusable = true;
			}
		}

		protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
		{
			base.OnItemsChanged(e);

			//если строка удаляет с помощью функции datagrid то после удалени фокус остается в datagrid
			//если же удаление производится из коллекции ItemsSource то CurrentItem сбрасывается в null
			//и таблица теряет фокус
			if (e.Action == NotifyCollectionChangedAction.Remove) {
				var index = Math.Min(e.OldStartingIndex, Items.Count - 1);
				if (index >= 0) {
					CurrentItem = Items[index];
					SelectedItem = Items[index];
				}
			}
		}

		protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
		{
			base.OnItemsSourceChanged(oldValue, newValue);

			//порядок вызово важен нужно сначала центрировать а потом ставить фокус
			if (SelectedItem != null) {
				//скорбная песнь, для того что бы вычислить вертикальное смещение
				//что бы отобразить строку по центру используется ViewportHeight
				//но при изменение ItemsSource меняется и ViewportHeight
				//но ViewportHeight будет пересчитан только когда будет обновлен layout
				//контрола (ArrangeOverride)
				//по этому мы говорим планировщику что нужно выполнить Centrify после того как он поделает все дела
				//это приводит к неприятному эффекту "дергания" когда таблица рисуется в одном положении
				//а затем почти мгновенно в другом
				ScrollIntoView(SelectedItem);
				Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => {
					DataGridHelper.Centrify(this);
				}));
			}

			//после обновление ItemsSource все визуальные элементы будут перестроены
			//и потеряют фокус
			//для того что бы восстановить фокус нужно запланировать это после того как новые элементы будут построены
			if(IsKeyboardFocusWithin) {
				Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => {
					DataGridHelper.Focus(this);
				}));
			}
		}
	}
}
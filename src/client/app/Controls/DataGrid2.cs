using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using Common.Tools;
using Action = System.Action;

namespace AnalitF.Net.Client.Controls
{
	public class DataGrid2 : DataGrid
	{
		public static DependencyProperty ShowAddressColumnProperty
			= DependencyProperty.RegisterAttached("ShowAddressColumn",
				typeof(bool),
				typeof(DataGrid2),
				new FrameworkPropertyMetadata(false, ShowAddressColumnPropertyChanged));

		//свойство нужно тк в 4.0
		//биндинг на CurrentItem сбрасывается http://connect.microsoft.com/visualstudio/feedback/details/696155/wpf
		public static DependencyProperty CurrentItemStubProperty
			= DependencyProperty.RegisterAttached("CurrentItemStub",
				typeof(object),
				typeof(DataGrid2),
				new FrameworkPropertyMetadata(null, CurrentItemStubChanged));

		private ScrollViewer viewer;

		static DataGrid2()
		{
			//DataGrid сбрасывает сортировку зачем непонятно
			ItemsSourceProperty.OverrideMetadata(typeof(DataGrid2), new FrameworkPropertyMetadata(null, OnCoerceItemsSourceProperty));
		}

		public DataGrid2()
		{
			//раньше для установки восстановления выделения использовалась перегрузка OnItemsChanged но
			//после обработки OnItemsChanged datagrid обрабатывает событие CollectionChanged
			//и в нем он сбрасывает выделение из внутреннего хранилища выделенных ячеек _selectedCells
			//сброс производится по индексу строки те строка удаляется -> выделенные ячейки удаляются из хранилища
			//после того как я перегрузил OnItemsChanged последовательность событий стала
			//строка удаляется -> выделение восстанавливается -> выделенные ячейки удаляются из хранилища
			//тк выделенные ячейки удаляются по индексу только что выделенная строка сама себе считает выделенной
			//но datagrid считает что у него нет выделенных ячеек
			//это приводит к тому что при переходе вверх или вниз визуально выделяются две строки
			((INotifyCollectionChanged)Items).CollectionChanged += CollectionChanged;
		}

		private static object OnCoerceItemsSourceProperty(DependencyObject d, object basevalue)
		{
			return basevalue;
		}

		public object CurrentItemStub
		{
			get { return GetValue(CurrentItemStubProperty); }
			set { SetValue(CurrentItemStubProperty, value); }
		}

		public bool ShowAddressColumn
		{
			get { return (bool)GetValue(ShowAddressColumnProperty); }
			set { SetValue(ShowAddressColumnProperty, value); }
		}

		public bool CanUserSelectMultipleItems
		{
			get { return CanSelectMultipleItems; }
			set { CanSelectMultipleItems = value; }
		}

		/*
		проблемы с фоксом
		1 - тк ScrollViewer Focusable то он может получить фокус если на него кликнуть
		это правильно когда таблица пуста для того что бы получать ввод с клавиатуры
		и не правильно когда в таблице есть элементы
		фокус должен оставаться на элементе
		*/
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			viewer = this.Descendants<ScrollViewer>().FirstOrDefault(s => s.Name == "DG_ScrollViewer");
			if (viewer != null) {
				//тк viewer может получить фокус это будет происходить даже тогда когда не надо
				//например в случае если пользователь кликнет по пустому полю таблицы
				//фокус получит viewer и ввод с клавиатуры уйдет к нему не дав ни какого эффекта
				viewer.GotKeyboardFocus += (s, a) => {
					if (viewer.IsKeyboardFocused && Items.Count > 0) {
						DataGridHelper.Focus(this);
					}
				};
				viewer.Focusable = true;
			}
		}

		private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			//если строка удаляет с помощью функции datagrid то после удалении фокус остается в datagrid
			//если же удаление производится из коллекции ItemsSource то CurrentItem сбрасывается в null
			//и таблица теряет фокус
			if (e.Action == NotifyCollectionChangedAction.Remove) {
				var index = Math.Min(e.OldStartingIndex, Items.Count - 1);
				if (index >= 0) {
					CurrentItem = Items[index];
					SelectedItem = Items[index];
				}
			}

			//если data grid получит событие Reset он убьет все ячейки и построит их заново
			//вместе с ячейками он убьет и фокус
			//Reset произойдет во множестве случаев в том числе если к данным применить сортировку Items.SortDescriptions.Add
			//что бы не терять фокус ловим событие и если владем фоксом восстанавливаем его после того как данные обновились
			if (e.Action == NotifyCollectionChangedAction.Reset && IsKeyboardFocusWithin) {
				Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => {
					DataGridHelper.Focus(this);
				}));
			}
		}

		protected static void CurrentItemStubChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var grid = (DataGrid2)d;
			grid.CurrentItem = e.NewValue;
		}

		//это хак, тк дата биндинг не работает для DataGridColumn
		//это фактически его эмуляция
		private static void ShowAddressColumnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var grid = (DataGrid2)d;
			var column = DataGridHelper.FindColumn(grid.Columns, "Адрес заказа");
			if (column == null)
				return;

			column.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
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
			else if (e.Key == Key.Home) {
				if (viewer != null) {
					viewer.ScrollToHome();
					SelectedItem = Items.Cast<object>().FirstOrDefault();
					e.Handled = true;
					return;
				}
			}
			else if (e.Key == Key.End) {
				if (viewer != null) {
					viewer.ScrollToEnd();
					SelectedItem = Items.Cast<object>().LastOrDefault();
					e.Handled = true;
					return;
				}
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

		protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
		{
			base.OnItemsSourceChanged(oldValue, newValue);

			//порядок вызова важен нужно сначала центрировать а потом ставить фокус
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

		protected override Size MeasureOverride(Size availableSize)
		{
			var size = base.MeasureOverride(availableSize);

			//в текущей реализации datagrid вычислении ширины колонок происходит асинхронно
			//после того как таблица была нарисована, это приводит к странному эффекту когда таблица рисуется
			//с одним размером колонок а затем почти мгновенно с другим
			//зачем так происходит понять невозможно, мне кажется это продолжение безумия
			//с асинхронной механикой рисования в wpf
			//ниже я вызываю приватные методы что бы вычислить ширину колонок до этапа рисования
			//это не решает проблему перерисовки полностью но решает ее значительную часть
			//перерисовка все равно производится но она затрагивает только заголовок таблицы
			//это неприятно не лучше чем было
			var value = GetProperty(this, "InternalColumns");
			Invoke(value, "ComputeColumnWidths");
			SetField(value, "_columnWidthsComputationPending", false);
			return size;
		}

		private void SetField(object target, string name, bool value)
		{
			var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
			f.SetValue(target, value);
		}

		private void Invoke(object value, string name)
		{
			var m = value.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic, null,
				new[] { typeof(object) }, null);
			m.Invoke(value, new object[] { null });
		}

		private object GetProperty(DataGrid2 dataGrid, string name)
		{
			var prop = typeof(DataGrid2).GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic);
			return prop.GetValue(dataGrid, null);
		}

		protected override void OnExecutedCancelEdit(ExecutedRoutedEventArgs e)
		{
			base.OnExecutedCancelEdit(e);

			//очередная проблема с datagrid
			//при выходе из режима редактирования
			//повторное нажатие escape обрабатывает как завершение редактирование хотя редактирование уже завершено
			//похоже что проблема состоит в неверном определение типа редактирования
			//у Items не сбрасывается флаг IsEditingItem и повторно нажатие escape интерпретируется как завершение
			//редактирования строки
			//тк на некоторых формах escape обрабатывается как выход из формы такое поведение неприемлемо
			var cell = DataGridHelper.GetCell(this, CurrentCell);
			if (cell == null || !cell.IsEditing) {
				e.Handled = false;
				var editable = ((IEditableCollectionView)Items);
				if (editable.IsEditingItem)
					editable.CancelEdit();
			}
		}
	}
}

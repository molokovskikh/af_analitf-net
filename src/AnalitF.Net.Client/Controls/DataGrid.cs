using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

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
			if (e.Key == Key.Enter)
				return;

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
	}
}
using System;
using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using AnalitF.Net.Client.Extentions;
using NHibernate.Util;

namespace AnalitF.Net.Client.Controls
{
	public class DataGrid : System.Windows.Controls.DataGrid
	{
		public DataGrid()
		{
			SetValue(IsEmptyProperty, false);
			var binding = new Binding("ItemsSource.Count") {Converter = new IntToBoolConverter(), FallbackValue = false, Source = this};
			SetBinding(IsEmptyProperty, binding);
		}

		public static readonly DependencyProperty IsEmptyProperty = DependencyProperty.RegisterAttached(
			"IsEmpty",
			typeof(Boolean),
			typeof(DataGrid),
			new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.None)
		);

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				return;

			base.OnKeyDown(e);
		}

		protected override void OnExecutedDelete(ExecutedRoutedEventArgs e)
		{
		}

		protected override void OnCanExecuteDelete(CanExecuteRoutedEventArgs e)
		{
			e.Handled = false;
			e.ContinueRouting = true;
		}

		public bool IsEmpty
		{
			get { return (bool)GetValue(IsEmptyProperty); }
			set { SetValue(IsEmptyProperty, value); }
		}
	}
}
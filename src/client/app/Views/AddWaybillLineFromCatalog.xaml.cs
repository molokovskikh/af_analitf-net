using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AnalitF.Net.Client.Views
{
	/// <summary>
	/// Interaction logic for SelectFromCatalog.xaml
	/// </summary>
	public partial class AddWaybillLineFromCatalog : UserControl
	{
		public AddWaybillLineFromCatalog()
		{
			InitializeComponent();

			IsVisibleChanged += AddWaybillLineFromCatalog_IsVisibleChanged;

			Catalogs.GotFocus += ComboBox_GotFocus;
			Producers.GotFocus += ComboBox_GotFocus;

			SupplierCost_Value.GotFocus += TextBox_GotFocus;
			Quantity_Value.GotFocus += TextBox_GotFocus;
		}

		private void ComboBox_GotFocus(object sender, RoutedEventArgs e)
		{
			(sender as ComboBox).IsDropDownOpen = true;
		}

		private void TextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			var textBox = sender as TextBox;
			textBox.SelectionStart = 0;
			textBox.SelectionLength = textBox.Text.Length;
		}

		private void AddWaybillLineFromCatalog_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.NewValue == false)
				return;
			Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(delegate() { Catalogs.Focus(); }));
		}
	}
}

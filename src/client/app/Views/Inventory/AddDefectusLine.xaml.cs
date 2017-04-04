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

namespace AnalitF.Net.Client.Views.Inventory
{
	/// <summary>
	/// Interaction logic for AddDefectusLine.xaml
	/// </summary>
	public partial class AddDefectusLine : UserControl
	{
		public AddDefectusLine()
		{
			InitializeComponent();

			IsVisibleChanged += AddDefectusLine_IsVisibleChanged;

			CatalogProducts.GotFocus += (sender, args) => {
				CatalogProducts.IsDropDownOpen = true;
			};

			CatalogProducts.DropDownClosed += (sender, args) => {
				if (CatalogProducts.IsKeyboardFocusWithin && CatalogProducts.SelectedItem != null)
					OK.Focus();
			};

			Producers.GotFocus += (sender, args) => {
				Producers.IsDropDownOpen = true;
			};

			Producers.DropDownClosed += (sender, args) => {
				if (Producers.IsKeyboardFocusWithin && Producers.SelectedItem != null)
					OK.Focus();
			};
		}

		private void AddDefectusLine_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.NewValue == false)
				return;
			Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(delegate () { CatalogProducts.Focus(); }));
		}
	}
}

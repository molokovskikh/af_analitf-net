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
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Views.Offers
{
	public partial class AddAwaited : UserControl
	{
		public AddAwaited()
		{
			InitializeComponent();

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
	}
}

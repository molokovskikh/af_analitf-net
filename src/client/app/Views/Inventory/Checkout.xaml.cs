using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace AnalitF.Net.Client.Views.Inventory
{
	public partial class Checkout : UserControl
	{
		public ViewModels.Inventory.Checkout Model => (ViewModels.Inventory.Checkout)DataContext;
		public Checkout()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				Amount.Focus();
				Amount.SelectAll();
			};
			KeyDown += (sender, args) => {
				if (args.Key == Key.Enter) {
					Model.OK();
				}
			};
		}
	}
}

using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AnalitF.Net.Client.Views.Inventory
{
	public partial class Checkout : UserControl
	{
		public ViewModels.Inventory.Checkout Model => DataContext as ViewModels.Inventory.Checkout;

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

			DataContextChanged += (sender, args) => {
				if (Model == null)
					return;

				Model.IsValid.Subscribe(x => {
					if (x)
						Change.ClearValue(Label.ForegroundProperty);
					else
						Change.Foreground = Brushes.Red;
				});
			};
		}
	}
}

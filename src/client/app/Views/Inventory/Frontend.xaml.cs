using System;
using System.Windows.Controls;
using System.Windows.Input;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Views.Inventory
{
	public partial class Frontend : UserControl
	{
		public ViewModels.Inventory.Frontend Model => (ViewModels.Inventory.Frontend)DataContext;

		public Frontend()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				Input.Focus();
			};
			PreviewKeyDown += (sender, args) => {
				if (args.Key == Key.Multiply) {
					Model.UpdateQuantity();
					args.Handled = true;
				} else if (args.Key == Key.F2) {
					Model.SearchByProductId();
				} else if (args.Key == Key.F3) {
					Model.SearchByBarcode();
				} else if (args.Key == Key.F6) {
					Coroutine.BeginExecute(Model.SearchByTerm().GetEnumerator(), new ActionExecutionContext { View = this });
				} else if (args.Key == Key.Enter) {
					Coroutine.BeginExecute(Model.Checkout().GetEnumerator(), new ActionExecutionContext { View = this });
				}
			};
		}
	}
}

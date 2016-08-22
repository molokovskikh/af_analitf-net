using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace AnalitF.Net.Client.Views.Inventory
{
	public partial class Frontend : UserControl
	{
		public ViewModels.Inventory.Frontend Model => (ViewModels.Inventory.Frontend)DataContext;

		public Frontend()
		{
			InitializeComponent();

			PreviewKeyDown += (sender, args) => {
				if (args.Key == Key.Multiply) {
					Model.UpdateQuantity();
				}
			};
			//Input.TextInput += (sender, args) => {
			//	if (args.)
			//};
		}
	}
}

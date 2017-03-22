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
using AnalitF.Net.Client.Helpers;
using Common.Tools;

namespace AnalitF.Net.Client.Views.Dialogs
{
	/// <summary>
	/// Interaction logic for EditStock.xaml
	/// </summary>
	public partial class EditStock : UserControl
	{
		public EditStock()
		{
			InitializeComponent();

			DataContextChanged += (sender, args) => {
				var model = DataContext as ViewModels.Dialogs.EditStock;
				if (model == null)
					return;
				if (model.EditMode == ViewModels.Dialogs.EditStock.Mode.EditQuantity) {
					this.Descendants<TextBox>().Each(x => x.IsReadOnly = true);
					Stock_Quantity.IsReadOnly = false;
				} else if (model.EditMode == ViewModels.Dialogs.EditStock.Mode.EditRetailCostAndQuantity) {
					this.Descendants<TextBox>().Each(x => x.IsReadOnly = true);
					Stock_Quantity.IsReadOnly = false;
					Stock_RetailCost.IsReadOnly = false;
					Stock_RetailMarkup.IsReadOnly = false;
				} else if (model.EditMode == ViewModels.Dialogs.EditStock.Mode.EditAll) {
					this.Descendants<TextBox>().Each(x => x.IsReadOnly = false);
				} else {
					Stock_Quantity.IsReadOnly = true;
				}
			};
		}
	}
}

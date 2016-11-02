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
using Caliburn.Micro;

namespace AnalitF.Net.Client.Views.Inventory
{
	/// <summary>
	/// Interaction logic for EditDisplacementDoc.xaml
	/// </summary>
	public partial class EditDisplacementDoc : UserControl
	{
		public ViewModels.Inventory.EditDisplacementDoc Model => DataContext as ViewModels.Inventory.EditDisplacementDoc;

		public EditDisplacementDoc()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				Lines.Focus();
			};

			Keyboard.AddPreviewKeyDownHandler(this, (sender, args) => {
				IEnumerable<IResult> results = null;
				if (args.Key == Key.Insert) {
					results = Model.AddLine();
					args.Handled = true;
				} else if (args.Key == Key.Delete) {
					Model.DeleteLine();
					args.Handled = true;
				}
				if (results != null) {
					Coroutine.BeginExecute(results.GetEnumerator(), new ActionExecutionContext { View = this });
					args.Handled = true;
				}
			});
		}
	}
}

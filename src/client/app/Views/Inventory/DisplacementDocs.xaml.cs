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
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.ViewModels;

namespace AnalitF.Net.Client.Views.Inventory
{
	/// <summary>
	/// Interaction logic for DisplacementDocs.xaml
	/// </summary>
	public partial class DisplacementDocs : UserControl
	{
		public ViewModels.Inventory.DisplacementDocs Model => DataContext as ViewModels.Inventory.DisplacementDocs;

		public DisplacementDocs()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				ApplyStyles();
				Items.Focus();
			};

			Items.CommandBindings.Add(new CommandBinding(DataGrid.DeleteCommand,
				Commands.DoInvokeViewModel,
				Commands.CanInvokeViewModel));

			Keyboard.AddPreviewKeyDownHandler(this, (sender, args) => {
				if (args.Key == Key.Insert) {
					Model.Create();
					args.Handled = true;
				} else if (args.Key == Key.Delete) {
					var task = Model.Delete();
					args.Handled = true;
				}
			});
		}
		public void ApplyStyles()
		{
			var context = "";
			if (((BaseScreen)DataContext).User != null)
				context = "CorrectionEnabled";
			StyleHelper.ApplyStyles(typeof(DisplacementDoc), Items, Application.Current.Resources, Legend, context);
		}
	}
}

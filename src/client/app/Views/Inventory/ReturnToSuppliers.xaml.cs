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
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Views.Inventory
{
	/// <summary>
	/// Interaction logic for ReturnToSuppliers.xaml
	/// </summary>
	public partial class ReturnToSuppliers : UserControl
	{
		public ReturnToSuppliers()
		{
			InitializeComponent();
			ApplyStyles();

			Items.CommandBindings.Add(new CommandBinding(DataGrid.DeleteCommand,
				Commands.DoInvokeViewModel,
				Commands.CanInvokeViewModel));
		}

		public void ApplyStyles()
		{
			StyleHelper.ApplyStyles(typeof(ReturnToSupplier), Items, Application.Current.Resources, Legend);
		}
	}
}

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Views
{
	public partial class WaybillsView : UserControl
	{
		public WaybillsView()
		{
			InitializeComponent();
			ApplyStyles();

			Waybills.CommandBindings.Add(new CommandBinding(DataGrid.DeleteCommand,
				Commands.DoInvokeViewModel,
				Commands.CanInvokeViewModel));
		}

		public void ApplyStyles()
		{
			StyleHelper.ApplyStyles(typeof(Waybill), Waybills, Application.Current.Resources, Legend);
		}
	}
}

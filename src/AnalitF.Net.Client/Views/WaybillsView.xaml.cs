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


			var type = typeof(Waybill);
			var resources = Application.Current.Resources;
			StyleHelper.ApplyStyles(type, Waybills, resources, Legend);

			Waybills.CommandBindings.Add(new CommandBinding(DataGrid.DeleteCommand,
				CommandBinder.InvokeViewModel,
				CommandBinder.CanInvokeViewModel));
		}
	}
}

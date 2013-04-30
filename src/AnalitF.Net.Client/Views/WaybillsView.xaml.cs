using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Binders;

namespace AnalitF.Net.Client.Views
{
	public partial class WaybillsView : UserControl
	{
		public WaybillsView()
		{
			InitializeComponent();

			Waybills.CommandBindings.Add(new CommandBinding(DataGrid.DeleteCommand,
				CommandBinder.InvokeViewModel,
				CommandBinder.CanInvokeViewModel));
		}
	}
}

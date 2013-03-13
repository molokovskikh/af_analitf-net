using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Binders;

namespace AnalitF.Net.Client.Views
{
	public partial class OrdersView : UserControl
	{
		public OrdersView()
		{
			InitializeComponent();

			ContextMenuBehavior.Attach(Orders);

			Orders.CommandBindings.Add(new CommandBinding(DataGrid.DeleteCommand,
				CommandBinder.InvokeViewModel,
				CommandBinder.CanInvokeViewModel));

			SentOrders.CommandBindings.Add(new CommandBinding(DataGrid.DeleteCommand,
				CommandBinder.InvokeViewModel,
				CommandBinder.CanInvokeViewModel));
		}
	}
}

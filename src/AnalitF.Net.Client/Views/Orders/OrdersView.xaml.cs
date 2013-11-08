using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Views
{
	public partial class OrdersView : UserControl
	{
		public OrdersView()
		{
			InitializeComponent();

			StyleHelper.ApplyStyles(typeof(Order), Orders, Application.Current.Resources, Legend);
			Orders.CommandBindings.Add(new CommandBinding(DataGrid.DeleteCommand,
				CommandBinder.InvokeViewModel,
				CommandBinder.CanInvokeViewModel));

			SentOrders.CommandBindings.Add(new CommandBinding(DataGrid.DeleteCommand,
				CommandBinder.InvokeViewModel,
				CommandBinder.CanInvokeViewModel));
		}
	}
}

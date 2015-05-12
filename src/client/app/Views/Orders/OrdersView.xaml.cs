using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Orders;

namespace AnalitF.Net.Client.Views.Orders
{
	public partial class OrdersView : UserControl
	{
		public OrdersView()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				ApplyStyles();
			};

			DataContextChanged += (sender, args) => {
				var model = DataContext as OrdersViewModel;
				if (model != null) {
					if (!model.User.HaveLimits)
						Orders.Columns.Remove(DataGridHelper.FindColumn(Orders, "Лимит"));
				}
			};


			Orders.CommandBindings.Add(new CommandBinding(DataGrid.DeleteCommand,
				Commands.DoInvokeViewModel,
				Commands.CanInvokeViewModel));

			SentOrders.CommandBindings.Add(new CommandBinding(DataGrid.DeleteCommand,
				Commands.DoInvokeViewModel,
				Commands.CanInvokeViewModel));
		}

		public void ApplyStyles()
		{
			var context = "";
			if (((BaseScreen)DataContext).User != null && ((BaseScreen)DataContext).User.IsPreprocessOrders)
				context = "CorrectionEnabled";
			StyleHelper.ApplyStyles(typeof(Order), Orders, Application.Current.Resources, Legend, context);
			StyleHelper.ApplyStyles(typeof(SentOrder), SentOrders, Application.Current.Resources, Legend);
		}
	}
}

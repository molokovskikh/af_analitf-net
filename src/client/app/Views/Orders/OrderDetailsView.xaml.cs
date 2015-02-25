using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Orders;

namespace AnalitF.Net.Client.Views.Orders
{
	public partial class OrderDetailsView : UserControl
	{
		public OrderDetailsView()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				ApplyStyles();
			};

			DataContextChanged += (sender, args) => {
				var model = DataContext as OrderDetailsViewModel;
				if (model != null) {
					if (!model.IsCurrentOrder)
						Lines.Columns.Remove(DataGridHelper.FindColumn(Lines, "Эффективность"));
				}
			};

			DataGridHelper.CalculateColumnWidths(Lines);
			new Editable().Attach(Lines);
		}

		public void ApplyStyles()
		{
			var context = "";
			var screen = ((OrderDetailsViewModel)DataContext);
			var user = screen.User;
			if (screen.Settings.Value == null)
				return;
			if (screen.IsCurrentOrder || !screen.Settings.Value.HighlightUnmatchedOrderLines) {
				if (user != null && user.IsPreprocessOrders)
					context = "CorrectionEnabled";
				StyleHelper.ApplyStyles(typeof(OrderLine), Lines, Application.Current.Resources, Legend, context);
			}
			else {
				StyleHelper.ApplyStyles(typeof(SentOrderLine), Lines, Application.Current.Resources, Legend);
			}
		}
	}
}

using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;

namespace AnalitF.Net.Client.Views.Orders
{
	public partial class OrderLinesView : UserControl
	{
		public OrderLinesView()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				ApplyStyles();
			};

			DataGridHelper.CalculateColumnWidths(Lines);
			DataGridHelper.CalculateColumnWidths(SentLines);
			new Editable().Attach(Lines);
		}

		public void ApplyStyles()
		{
			var context = "";
			var baseScreen = ((BaseScreen)DataContext);
			if (baseScreen.User != null && baseScreen.User.IsPreprocessOrders)
				context = "CorrectionEnabled";
			StyleHelper.ApplyStyles(typeof(OrderLine), Lines, Application.Current.Resources, Legend, context);

			if (baseScreen.Settings.Value != null && baseScreen.Settings.Value.HighlightUnmatchedOrderLines)
				StyleHelper.ApplyStyles(typeof(SentOrderLine), SentLines, Application.Current.Resources, Legend);
		}
	}
}

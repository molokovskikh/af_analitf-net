using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;

namespace AnalitF.Net.Client.Views
{
	public partial class CatalogOfferView : UserControl
	{
		public CatalogOfferView()
		{
			InitializeComponent();
			var grid = Offers;
			Loaded += (sender, args) => {
				var model = DataContext as CatalogOfferViewModel;
				if (model != null && model.IsFilterByCatalogName)
					Offers.Items.GroupDescriptions.Add(new PropertyGroupDescription("GroupName"));
			};

			new Editable().Attach(grid);
			DataGridHelper.CalculateColumnWidths(Offers);
			ApplyStyles();
			BindingOperations.SetBinding(OfferOverlayPanel, Grid.MaxHeightProperty,
				new Binding("ActualHeight") {
					Source = Offers,
					Converter = new LambdaConverter<double>(v => v * 0.7)
				});
		}

		public void ApplyStyles()
		{
			StyleHelper.ApplyStyles(typeof(Offer), Offers, Application.Current.Resources, Legend);
			StyleHelper.ApplyStyles(typeof(SentOrderLine), HistoryOrders, Application.Current.Resources);
		}
	}
}

using System.Windows.Controls;
using System.Windows.Data;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;
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

			EditBehavior.Attach(grid);

			Offers.TextInput += (sender, args) => {
				args.Handled = true;
				var model = DataContext as CatalogOfferViewModel;
				if (model != null)
					model.SearchInCatalog(args.Text);
			};

			DataGridHelper.CalculateColumnWidths(Offers);
		}
	}
}

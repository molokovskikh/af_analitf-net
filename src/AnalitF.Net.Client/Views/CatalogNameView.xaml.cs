using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Views
{
	public partial class CatalogNameView : UserControl
	{
		public CatalogNameView()
		{
			InitializeComponent();

			SizeChanged += (sender, args) => {
				CatalogNamesColumn.MaxWidth = args.NewSize.Width / 2;
			};

			Catalogs.KeyDown += (sender, args) => {
				if (args.Key == Key.Escape) {
					DataGridHelper.Focus(CatalogNames);
					args.Handled = true;
				}
			};
		}
	}
}

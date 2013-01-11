using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Views
{
	public partial class CatalogSearchView : UserControl
	{
		public CatalogSearchView()
		{
			InitializeComponent();

			QuickSearchBehavior.AttachSearch(Catalogs, QuickSearch_SearchText);
		}
	}
}

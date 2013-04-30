using System.Windows.Controls;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Views
{
	public partial class JunkOfferView : UserControl
	{
		public JunkOfferView()
		{
			InitializeComponent();
			EditBehavior.Attach(Offers);
			QuickSearchBehavior.AttachSearch(Offers, QuickSearch_SearchText);
			DataGridHelper.CalculateColumnWidths(Offers);
		}
	}
}

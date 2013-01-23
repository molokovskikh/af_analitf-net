using System.Windows.Controls;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Views
{
	public partial class SearchOfferView : UserControl
	{
		public SearchOfferView()
		{
			InitializeComponent();

			EditBehavior.Attach(Offers);
			ContextMenuBehavior.Attach(Offers);
			DataGridHelper.CalculateColumnWidths(Offers);
		}
	}
}

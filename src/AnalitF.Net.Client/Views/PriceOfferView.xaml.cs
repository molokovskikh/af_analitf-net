using System.Windows.Controls;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Views
{
	public partial class PriceOfferView : UserControl
	{
		public PriceOfferView()
		{
			InitializeComponent();

			EditBehavior.Attach(Offers);
			ContextMenuBehavior.Attach(Offers);
			DataGridHelper.CalculateColumnWidths(Offers);
		}
	}
}

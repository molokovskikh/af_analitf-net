using System.Windows.Controls;
using AnalitF.Net.Client.Binders;

namespace AnalitF.Net.Client.Views
{
	public partial class PriceOfferView : UserControl
	{
		public PriceOfferView()
		{
			InitializeComponent();

			EditBehavior.Attach(Offers);
			ContextMenuBehavior.Attach(Offers);
		}
	}
}

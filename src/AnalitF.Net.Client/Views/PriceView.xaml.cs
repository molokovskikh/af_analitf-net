using System.Windows.Controls;
using AnalitF.Net.Client.Binders;

namespace AnalitF.Net.Client.Views
{
	public partial class PriceView : UserControl
	{
		public PriceView()
		{
			InitializeComponent();
			ContextMenuBehavior.Attach(Prices);
			QuickSearchBehavior.AttachSearch(Prices, QuickSearch_SearchText);
		}
	}
}

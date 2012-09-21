using System.Windows.Controls;
using AnalitF.Net.Client.Extentions;

namespace AnalitF.Net.Client.Views
{
	public partial class OfferView : UserControl
	{
		public OfferView()
		{
			InitializeComponent();
			Loaded += (sender, args) => {
				XamlExtentions.Focus(Offers);
			};
		}
	}
}

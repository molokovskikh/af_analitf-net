using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Views
{
	public partial class PriceOfferView : UserControl
	{
		public PriceOfferView()
		{
			InitializeComponent();

			EditBehavior.Attach(Offers);
			DataGridHelper.CalculateColumnWidths(Offers);
			StyleHelper.ApplyStyles(typeof(Offer), Offers, Application.Current.Resources, Legend);
		}
	}
}

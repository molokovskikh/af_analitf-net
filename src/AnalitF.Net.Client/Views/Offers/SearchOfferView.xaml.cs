using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Views
{
	public partial class SearchOfferView : UserControl
	{
		public SearchOfferView()
		{
			InitializeComponent();

			new Editable().Attach(Offers);
			DataGridHelper.CalculateColumnWidths(Offers);
			StyleHelper.ApplyStyles(typeof(Offer), Offers, Application.Current.Resources, Legend);

			StyleHelper.ApplyStyles(typeof(SentOrderLine), HistoryOrders, Application.Current.Resources);
		}
	}
}

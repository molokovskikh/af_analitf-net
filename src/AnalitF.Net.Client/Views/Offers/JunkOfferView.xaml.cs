using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Views
{
	public partial class JunkOfferView : UserControl
	{
		public JunkOfferView()
		{
			InitializeComponent();

			new Editable().Attach(Offers);
			DataGridHelper.CalculateColumnWidths(Offers);
			StyleHelper.ApplyStyles(typeof(Offer), Offers, Application.Current.Resources);

			BindingOperations.SetBinding(OfferOverlayPanel, Grid.MaxHeightProperty,
				new Binding("ActualHeight") {
					Source = Offers,
					Converter = new LambdaConverter<double>(v => v * 0.7)
				});
		}
	}
}

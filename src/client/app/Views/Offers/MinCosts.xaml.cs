using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Views.Orders;

namespace AnalitF.Net.Client.Views.Offers
{
	public partial class MinCosts : UserControl, IPersistable
	{
		public MinCosts()
		{
			InitializeComponent();

			Persister = new ViewPersister(this);
			Loaded += (sender, args) => {
				ApplyStyles();
				Persister.Track(MainGrid.RowDefinitions[Grid.GetRow(Costs)], RowDefinition.HeightProperty);
				Persister.Track(MainGrid.RowDefinitions[Grid.GetRow(Offers)], RowDefinition.HeightProperty);
				Persister.Restore();
			};

			new Editable().Attach(Offers);
			DataGridHelper.CalculateColumnWidths(Offers);
			ApplyStyles();
			BindingOperations.SetBinding(OfferOverlayPanel, Grid.MaxHeightProperty,
				new Binding("ActualHeight") {
					Source = Offers,
					Converter = new LambdaConverter<double>(v => v * 0.7)
				});
		}

		public ViewPersister Persister { get; }

		public void ApplyStyles()
		{
			StyleHelper.ApplyStyles(typeof(Offer), Offers, Application.Current.Resources);
		}
	}
}

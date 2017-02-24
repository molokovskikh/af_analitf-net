using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using System.Windows.Input;
using AnalitF.Net.Client.ViewModels.Offers;

namespace AnalitF.Net.Client.Views.Offers
{
	public partial class CatalogOfferView : UserControl
	{
		public CatalogOfferView()
		{
			InitializeComponent();
			var grid = Offers;

			grid.CommandBindings.Add(new CommandBinding(DataGrid.DeleteCommand,
				Commands.DoInvokeViewModel,
				Commands.CanInvokeViewModel));

			Conventions.ConfigureDataGrid(grid, typeof(Offer));
			new Editable().Attach(grid);
			DataGridHelper.CalculateColumnWidths(Offers);
			ApplyStyles();
			BindingOperations.SetBinding(OfferOverlayPanel, Grid.MaxHeightProperty,
				new Binding("ActualHeight") {
					Source = Offers,
					Converter = new LambdaConverter<double>(v => v * 0.7)
				});

			var element = Rounding;
			var items = DescriptionHelper.GetDescriptions(typeof(Rounding));
			element.ItemsSource = items;
			element.DisplayMemberPath = "Name";

			var binding = new Binding("Rounding.Value") {
				Converter = new ComboBoxSelectedItemConverter(),
				ConverterParameter = items
			};
			BindingOperations.SetBinding(element, Selector.SelectedItemProperty, binding);
		}

		public void ApplyStyles()
		{
			StyleHelper.ApplyStyles(typeof(Offer), Offers, Application.Current.Resources, Legend);
			StyleHelper.ApplyStyles(typeof(SentOrderLine), HistoryOrders, Application.Current.Resources);
		}
	}
}

﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Views.Offers
{
	public partial class SearchOfferView : UserControl
	{
		public SearchOfferView()
		{
			InitializeComponent();

			new Editable().Attach(Offers);
			DataGridHelper.CalculateColumnWidths(Offers);
			ApplyStyles();

			BindingOperations.SetBinding(OfferOverlayPanel, Grid.MaxHeightProperty,
				new Binding("ActualHeight") {
					Source = Offers,
					Converter = new LambdaConverter<double>(v => v * 0.7)
				});
			SearchText.KeyDown += (sender, args) => {
				if (args.Key == Key.Return) {
					DataGridHelper.Focus(Offers);
				}
			};

			ProductInfo.ShowDescription.Content = "Описание (F1)";
		}

		public void ApplyStyles()
		{
			StyleHelper.ApplyStyles(typeof(Offer), Offers, Application.Current.Resources, Legend);
			StyleHelper.ApplyStyles(typeof(SentOrderLine), HistoryOrders, Application.Current.Resources);
		}
	}
}

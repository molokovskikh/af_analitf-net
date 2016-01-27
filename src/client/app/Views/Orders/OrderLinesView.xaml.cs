using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace AnalitF.Net.Client.Views.Orders
{
	public partial class OrderLinesView : UserControl, IPersistable
	{
		private GridLength height = new GridLength(1, GridUnitType.Star);

		public OrderLinesView()
		{
			InitializeComponent();
			Persister = new ViewPersister(this);

			Loaded += (sender, args) => {
				ApplyStyles();
				Persister.Track(Expander, Expander.IsExpandedProperty);
				Persister.Track(OrdersGrid.RowDefinitions[Grid.GetRow(Expander)], RowDefinition.HeightProperty);
				Persister.Track(OrdersGrid.RowDefinitions[Grid.GetRow(Lines)], RowDefinition.HeightProperty);
				Persister.Restore();
			};

			DataGridHelper.CalculateColumnWidths(Lines);
			DataGridHelper.CalculateColumnWidths(SentLines);
			new Editable().Attach(Lines);

			new Editable().Attach(Offers);
			DataGridHelper.CalculateColumnWidths(Offers);
			ExpandedCollapsed(Expander, null);
		}

		public ViewPersister Persister { get; }

		public void ApplyStyles()
		{
			var context = "";
			var baseScreen = (BaseScreen)DataContext;
			if (baseScreen.User != null && baseScreen.User.IsPreprocessOrders)
				context = "CorrectionEnabled";
			StyleHelper.ApplyStyles(typeof(OrderLine), Lines, Application.Current.Resources, Legend, context);

			if (baseScreen.Settings.Value != null && baseScreen.Settings.Value.HighlightUnmatchedOrderLines)
				StyleHelper.ApplyStyles(typeof(SentOrderLine), SentLines, Application.Current.Resources, Legend);

			StyleHelper.ApplyStyles(typeof(Offer), Offers, Application.Current.Resources, Legend);
		}

		private void ExpandedCollapsed(object sender, RoutedEventArgs e)
		{
			var expander = (Expander)sender;
			var row = OrdersGrid.RowDefinitions[Grid.GetRow(expander)];
			if (expander.IsExpanded) {
				row.Height = height;
			}
			else {
				height = row.Height;
				row.Height = GridLength.Auto;
			}
		}
	}
}

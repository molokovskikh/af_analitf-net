﻿using System;
using System.Net.Mime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Views.Orders
{
	public partial class Correction : UserControl
	{
		public Correction()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				var context = (ViewModels.Orders.Correction)DataContext;
				if (context.IsUpdate)
					Lines.Items.GroupDescriptions.Add(new PropertyGroupDescription("Order.Address"));
				Lines.Items.GroupDescriptions.Add(new PropertyGroupDescription("Order"));

				if (context.IsOrderSend) {
					OffersRow.Height = GridLength.Auto;
				}
			};
			Lines.GroupStyleSelector = (@group, level) => {
				var context = (ViewModels.Orders.Correction)DataContext;
				if (context == null)
					return null;
				if (context.IsUpdate && level == 0) {
					return (GroupStyle)Lines.Resources["AddressGroup"];
				}

				return (GroupStyle)Lines.Resources["OrderGroup"];
			};

			ApplyStyles();

			new Editable().Attach(Offers);
			DataGridHelper.CalculateColumnWidths(Offers);
		}

		public void ApplyStyles()
		{
			StyleHelper.ApplyStyles(typeof(OrderLine), Lines, Application.Current.Resources, context: "Correction");
			StyleHelper.ApplyStyles(typeof(Offer), Offers, Application.Current.Resources);
		}
	}
}

﻿using System.Net.Mime;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;

namespace AnalitF.Net.Client.Views
{
	public partial class OrderDetailsView : UserControl
	{
		public OrderDetailsView()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				var context = "";
				if (((BaseScreen)DataContext).User != null && ((BaseScreen)DataContext).User.IsPreprocessOrders)
					context = "CorrectionEnabled";
				StyleHelper.ApplyStyles(typeof(OrderLine), Lines, Application.Current.Resources, Legend, context);
			};

			DataGridHelper.CalculateColumnWidths(Lines);
			new Editable().Attach(Lines);
		}
	}
}
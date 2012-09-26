﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AnalitF.Net.Client.Extentions;

namespace AnalitF.Net.Client.Views
{
	/// <summary>
	/// Interaction logic for PriceOfferView.xaml
	/// </summary>
	public partial class PriceOfferView : UserControl
	{
		public PriceOfferView()
		{
			InitializeComponent();
			Loaded += (sender, args) => {
				XamlExtentions.Focus(Offers);
			};
		}
	}
}

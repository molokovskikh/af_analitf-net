﻿using System.Net.Mime;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Views
{
	public partial class PriceView : UserControl
	{
		public PriceView()
		{
			InitializeComponent();
			StyleHelper.ApplyStyles(typeof(Price), Prices, Application.Current.Resources);
		}
	}
}

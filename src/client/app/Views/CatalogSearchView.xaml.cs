﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels;

namespace AnalitF.Net.Client.Views
{
	public partial class CatalogSearchView : UserControl
	{
		public CatalogSearchView()
		{
			InitializeComponent();
			StyleHelper.ApplyStyles(typeof(CatalogDisplayItem), Items, Application.Current.Resources);
		}
	}
}
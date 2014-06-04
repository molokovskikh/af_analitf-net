using System;
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
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Views
{
	public partial class WaybillLineSearchView : UserControl
	{
		public WaybillLineSearchView()
		{
			InitializeComponent();

			//в xaml назначение имени для колонки не работает
			CertificateLink.SetValue(NameProperty, "CertificateLink");
			ApplyStyles();
		}

		public void ApplyStyles()
		{
			StyleHelper.ApplyStyles(typeof(WaybillLine), Lines, Application.Current.Resources);
		}
	}
}

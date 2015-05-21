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
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Views
{
	public partial class OrderRejectDetails : UserControl
	{
		public OrderRejectDetails()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				ApplyStyles();
			};


			new Editable().Attach(Offers);
			DataGridHelper.CalculateColumnWidths(Offers);
		}

		public void ApplyStyles()
		{
			StyleHelper.ApplyStyles(typeof(Offer), Offers, Application.Current.Resources, Legend);
		}
	}
}

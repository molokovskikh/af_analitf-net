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
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.Helpers;
using DataGrid = AnalitF.Net.Client.Controls.DataGrid;

namespace AnalitF.Net.Client.Views
{
	public partial class PriceView : UserControl
	{
		public PriceView()
		{
			InitializeComponent();
			Loaded += (sender, args) => {
				DataGridHelper.Focus(Prices);
			};

			ContextMenuBehavior.Attach(Prices);
		}
	}
}

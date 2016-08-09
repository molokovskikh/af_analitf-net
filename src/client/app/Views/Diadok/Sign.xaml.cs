using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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

namespace AnalitF.Net.Client.Views.Diadok
{
	/// <summary>
	/// Interaction logic for Sign.xaml
	/// </summary>
	public partial class Sign : UserControl
	{
		public Sign()
		{
			InitializeComponent();
			Save.Click += Save_Click;
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if (ProductReciver.IsExpanded) {
				if(!AcceptedData.BindingGroup.CommitEdit())
					e.Handled = true;
				if(ByAttorney.IsChecked == true && !ByAttorneyData.BindingGroup.CommitEdit())
					e.Handled = true;
			}
		}
	}
}

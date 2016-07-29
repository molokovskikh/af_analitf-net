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

namespace AnalitF.Net.Client.Views.Inventory
{
	/// <summary>
	/// Interaction logic for Checks.xaml
	/// </summary>
	public partial class Checks : UserControl
	{
		public Checks()
		{
			InitializeComponent();

			SearchText.KeyDown += (sender, args) => {
				if (args.Key == Key.Return) {
					DataGridHelper.Focus(Items);
				}
			};
		}
	}
}

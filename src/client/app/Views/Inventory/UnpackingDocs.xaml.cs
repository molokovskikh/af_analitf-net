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
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Views.Inventory
{
	/// <summary>
	/// Interaction logic for UnpackingDocs.xaml
	/// </summary>
	public partial class UnpackingDocs : UserControl
	{
		public UnpackingDocs()
		{
			InitializeComponent();
			ApplyStyles();
		}

		public void ApplyStyles()
		{
			StyleHelper.ApplyStyles(typeof(UnpackingDoc), Items, Application.Current.Resources, Legend);
		}
	}
}

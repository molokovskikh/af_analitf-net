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

namespace AnalitF.Net.Client.Views.Diadok
{
	/// <summary>
	/// Interaction logic for ExtDocs.xaml
	/// </summary>
	public partial class Index : UserControl
	{
		public Index()
		{
			InitializeComponent();
			Documents.SizeChanged += Documents_SizeChanged;
		}

		private void Documents_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			ListView lv = (ListView)e.Source;
			Size ns = e.NewSize;
			GridView cols = lv.View as GridView;

			double[] absize = new double[] { 0.20, 0.25, 0.25, 0.15, 0.12 };
			for(int i = 0; i < cols.Columns.Count && i < absize.Length; i++)
			{
				cols.Columns[i].Width = ns.Width * absize[i];
			}
		}
	}
}

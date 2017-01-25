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
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Print;
using Common.Tools;

namespace AnalitF.Net.Client.Views.Inventory
{
	public partial class Tags : UserControl
	{
		public Tags()
		{
			InitializeComponent();

			Lines.Columns.Insert(0, DataGridHelper.CheckBoxColumn("", "Selected",
				x => Lines.Items.OfType<TagPrintable>().Each(y => y.Selected = x),
				true));
		}
	}
}

using System;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Views
{
	public partial class Main : UserControl
	{
		public Main()
		{
			InitializeComponent();
			DataGridHelper.CalculateColumnWidth(Newses, "0000.00.00", "Дата");
		}
	}
}

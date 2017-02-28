using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AnalitF.Net.Client.Controls
{
	public partial class WinFormGrid : UserControl
	{
		public DataGridView Grid
		{ get { return GridEx; } }

		public WinFormGrid()
		{
			InitializeComponent();
		}
	}
}

using System;
using System.Windows.Input;

namespace AnalitF.Net.Client.Controls
{
	public class DataGrid : System.Windows.Controls.DataGrid
	{
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Return)
				return;
			if (e.Key == Key.Escape)
				return;
			base.OnKeyDown(e);
		}
	}
}
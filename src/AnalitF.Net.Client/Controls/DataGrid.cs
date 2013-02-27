using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AnalitF.Net.Client.Controls
{
	public class DataGrid : System.Windows.Controls.DataGrid
	{
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				return;

			base.OnKeyDown(e);
		}

		protected override void OnExecutedDelete(ExecutedRoutedEventArgs e)
		{
		}

		protected override void OnCanExecuteDelete(CanExecuteRoutedEventArgs e)
		{
			e.Handled = false;
			e.ContinueRouting = true;
		}
	}
}
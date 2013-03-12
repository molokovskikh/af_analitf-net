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

		public bool BindDelete { get; set; }

		protected override void OnExecutedDelete(ExecutedRoutedEventArgs e)
		{
			if (BindDelete)
				base.OnExecutedDelete(e);
		}

		protected override void OnCanExecuteDelete(CanExecuteRoutedEventArgs e)
		{
			if (BindDelete) {
				base.OnCanExecuteDelete(e);
			}
			else {
				e.Handled = false;
				e.ContinueRouting = true;
			}
		}
	}
}
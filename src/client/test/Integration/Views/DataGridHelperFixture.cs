using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using NUnit.Framework;
using WpfHelper = AnalitF.Net.Client.Test.TestHelpers.WpfHelper;

namespace AnalitF.Net.Client.Test.Integration.Views
{
	[TestFixture]
	public class DataGridHelperFixture
	{
		[Test]
		public void Get_cell()
		{
			WpfHelper.WithWindow(w => {
				var grid = new DataGrid {
					Columns = {
						new DataGridTextColumn {
							DisplayIndex = 1
						},
						new DataGridTextColumn {
							DisplayIndex = 0
						}
					},
					ItemsSource = new [] { 1, 2 }
				};
				grid.CurrentItem = grid.Items[0];
				grid.CurrentCell = new DataGridCellInfo(grid.CurrentItem, grid.Columns[1]);
				w.Content = grid;
				grid.Loaded += (sender, args) => {
					var cell = DataGridHelper.GetCell(grid, grid.CurrentCell);
					Assert.AreEqual(grid.CurrentItem, cell.DataContext);
					Assert.AreEqual(grid.Columns[1], cell.Column);
					WpfHelper.Shutdown(w);
				};
			});
		}
	}
}
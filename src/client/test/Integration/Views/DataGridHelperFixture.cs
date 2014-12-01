using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Test.TestHelpers;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.Views
{
	[TestFixture]
	public class DataGridHelperFixture
	{
		[Test]
		public void Get_cell()
		{
			WpfTestHelper.WithWindow2(async w => {
				var grid = new DataGrid {
					Columns = {
						new DataGridTextColumn {
							DisplayIndex = 1
						},
						new DataGridTextColumn {
							DisplayIndex = 0
						}
					},
					ItemsSource = new[] { 1, 2 }
				};
				grid.CurrentItem = grid.Items[0];
				grid.CurrentCell = new DataGridCellInfo(grid.CurrentItem, grid.Columns[1]);
				w.Content = grid;
				await grid.WaitLoaded();
				var cell = DataGridHelper.GetCell(grid, grid.CurrentCell);
				Assert.AreEqual(grid.CurrentItem, cell.DataContext);
				Assert.AreEqual(grid.Columns[1], cell.Column);
			});
		}
	}
}
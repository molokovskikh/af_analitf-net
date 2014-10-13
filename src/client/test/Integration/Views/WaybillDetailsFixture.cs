using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using Common.Tools;
using NUnit.Framework;
using WpfHelper = AnalitF.Net.Client.Test.TestHelpers.WpfHelper;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture]
	public class WaybillDetailsFixture : BaseViewFixture
	{
		[Test]
		public void Set_cell_style()
		{
			var view = new WaybillDetailsView();
			var grid = (DataGrid2)view.FindName("Lines");
			var size = new Size(1000, 1000);
			view.Measure(size);
			view.Arrange(new Rect(size));

			Assert.IsNotNull(grid.CellStyle);
		}

		[Test]
		public void Auto_edit()
		{
			var waybill = Fixture<LocalWaybill>().Waybill;
			WpfHelper.WithWindow(w => {
				var model = new WaybillDetails(waybill.Id);
				var view = (WaybillDetailsView)Bind(model);
				w.Content = view;

				var grid = (DataGrid2)view.FindName("Lines");
				grid.Loaded += (sender, args) => {
					grid.SelectedItem = waybill.Lines[0];
					grid.RaiseEvent(WpfHelper.TextArgs("1"));
					var column = grid.Columns.First(c => c.Header is TextBlock && ((TextBlock)c.Header).Text.Equals("Розничная наценка"));
					Console.WriteLine();
					var cell = DataGridHelper.GetCell(
						(DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(grid.CurrentCell.Item),
						column,
						grid.Columns);
					Assert.IsTrue(cell.IsEditing);
					Assert.AreEqual("1", ((TextBox)cell.Content).Text);

					WpfHelper.Shutdown(w);
				};
			});
		}
	}
}
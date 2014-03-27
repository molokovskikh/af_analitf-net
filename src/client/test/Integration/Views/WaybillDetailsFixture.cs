using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.Views;
using Common.Tools;
using NUnit.Framework;
using WpfHelper = AnalitF.Net.Client.Test.TestHelpers.WpfHelper;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture]
	public class WaybillDetailsFixture
	{
		private WaybillDetailsView view;
		private DataGrid2 grid;

		[SetUp]
		public void BaseViewFixtureSetup()
		{
			ViewSetup.BindingErrors.Clear();
			ViewSetup.Setup();

			view = new WaybillDetailsView();
			grid = (DataGrid2)view.FindName("Lines");
			var size = new Size(1000, 1000);
			view.Measure(size);
			view.Arrange(new Rect(size));
		}

		[TearDown]
		public void TearDown()
		{
			if (ViewSetup.BindingErrors.Count > 0) {
				throw new Exception(ViewSetup.BindingErrors.Implode(Environment.NewLine));
			}
		}

		[Test]
		public void Set_cell_style()
		{
			Assert.IsNotNull(grid.CellStyle);
		}

		[Test, Explicit]
		public void Auto_edit()
		{
			var isEditing = false;
			var text = "";
			WpfHelper.WithWindow(w => {
				view = new WaybillDetailsView();
				grid = (DataGrid2)view.FindName("Lines");

				w.Content = view;
				var waybill = new Waybill();
				waybill.Lines.Add(new WaybillLine(waybill));
				grid.ItemsSource = waybill.Lines;

				grid.Loaded += (sender, args) => {
					grid.SelectedItem = waybill.Lines[0];
					grid.RaiseEvent(WpfHelper.TextArgs("1"));
					var column = grid.Columns.First(c => c.Header.Equals("Розничная наценка"));
					var cell = DataGridHelper.GetCell(
						(DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(grid.CurrentCell.Item),
						column);
					isEditing = cell.IsEditing;
					text = ((TextBox)cell.Content).Text;

					WpfHelper.Shutdown(w);
				};
			});
			Assert.IsTrue(isEditing);
			Assert.AreEqual("1", text);
		}
	}
}
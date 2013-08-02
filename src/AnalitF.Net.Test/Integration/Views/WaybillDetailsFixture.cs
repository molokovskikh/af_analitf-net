using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.Views;
using Common.Tools;
using NUnit.Framework;
using DataGrid = AnalitF.Net.Client.Controls.DataGrid;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture, RequiresSTA]
	public class WaybillDetailsFixture
	{
		private WaybillDetailsView view;
		private DataGrid grid;

		[SetUp]
		public void BaseViewFixtureSetup()
		{
			ViewFixtureSetup.BindingErrors.Clear();
			ViewFixtureSetup.Setup();

			view = new WaybillDetailsView();
			grid = (DataGrid)view.FindName("Lines");
			var size = new Size(1000, 1000);
			view.Measure(size);
			view.Arrange(new Rect(size));
		}

		[TearDown]
		public void TearDown()
		{
			if (ViewFixtureSetup.BindingErrors.Count > 0) {
				throw new Exception(ViewFixtureSetup.BindingErrors.Implode(Environment.NewLine));
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
				grid = (DataGrid)view.FindName("Lines");

				w.Content = view;
				var waybill = new Waybill();
				waybill.Lines.Add(new WaybillLine(waybill));
				grid.ItemsSource = waybill.Lines;

				grid.Loaded += (sender, args) => {
					grid.SelectedItem = waybill.Lines[0];
					grid.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, new TextComposition(null, null, "1")) {
						RoutedEvent = DataGrid.TextInputEvent
					});
					var column = grid.Columns.First(c => c.Header.Equals("Розничная наценка"));
					var cell = DataGridHelper.GetCell(
						(DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(grid.CurrentCell.Item),
						column);
					isEditing = cell.IsEditing;
					text = ((TextBox)cell.Content).Text;
				};
			});
			Assert.IsTrue(isEditing);
			Assert.AreEqual("1", text);
		}
	}
}
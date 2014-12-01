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
			WpfTestHelper.WithWindow2(async w => {
				var model = new WaybillDetails(waybill.Id);
				var view = (WaybillDetailsView)Bind(model);
				w.Content = view;

				var grid = (DataGrid2)view.FindName("Lines");
				await grid.WaitLoaded();
				grid.SelectedItem = waybill.Lines[0];
				grid.RaiseEvent(WpfTestHelper.TextArgs("1"));
				var column = grid.Columns.First(c => c.Header is TextBlock && ((TextBlock)c.Header).Text.Equals("Розничная наценка"));
				var cell = DataGridHelper.GetCell(
					(DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(grid.CurrentCell.Item),
					column,
					grid.Columns);
				Assert.IsTrue(cell.IsEditing);
				Assert.AreEqual("1", ((TextBox)cell.Content).Text);
			});

			//на форме корректировки могут возникнуть ошибки биндинга
			//судя по обсуждению это ошибки wpf и они безобидны
			//http://wpf.codeplex.com/discussions/47047
			//игнорирую их
			var ignored = new[] {
				"System.Windows.Data Error: 4",
				"Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.DataGrid', AncestorLevel='1''. BindingExpression:Path=AreRowDetailsFrozen; DataItem=null; target element is 'DataGridDetailsPresenter' (Name=''); target property is 'SelectiveScrollingOrientation' (type 'SelectiveScrollingOrientation')",
				"Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.DataGrid', AncestorLevel='1''. BindingExpression:Path=HeadersVisibility; DataItem=null; target element is 'DataGridRowHeader' (Name=''); target property is 'Visibility' (type 'Visibility')",
				//todo - разобрать причину ошибки
				"Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.DataGrid', AncestorLevel='1''. BindingExpression:Path=NewItemMargin; DataItem=null; target element is 'DataGridRow' (Name=''); target property is 'Margin' (type 'Thickness')"
			};
			ViewSetup.BindingErrors.RemoveAll(s => ignored.Any(m => s.Contains(m)));
		}
	}
}
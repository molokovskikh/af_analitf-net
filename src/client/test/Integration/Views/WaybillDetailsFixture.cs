using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.Views
{
	[TestFixture]
	public class WaybillDetailsFixture : BaseViewFixture
	{
		[Test]
		public void Set_cell_style()
		{
			var view = new WaybillDetailsView();
			var grid = view.Descendants<DataGrid>().First(g => g.Name == "Lines");
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

				var grid = view.Descendants<DataGrid>().First(g => g.Name == "Lines");
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

			WpfTestHelper.CleanSafeError();
		}

		[Test]
		public void Show_print_for_created_waybill()
		{
			var waybill = new Waybill(session.Query<Address>().First(), session.Query<Supplier>().First());
			waybill.IsCreatedByUser = true;
			session.Save(waybill);

			WpfTestHelper.WithWindow2(async w => {
				var model = new WaybillDetails(waybill.Id);
				var view = (WaybillDetailsView)Bind(model);
				w.Content = view;

				await view.WaitLoaded();
				view.Descendants<Button>().First(b => b.Name == "PrintWaybill")
					.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
			});
			WpfTestHelper.CleanSafeError();
		}
	}
}
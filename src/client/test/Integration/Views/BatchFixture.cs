using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Orders;
using Common.NHibernate;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.Views
{
	[TestFixture]
	public class BatchFixture : BaseViewFixture
	{
		[Test]
		public void Show_address()
		{
			restore = true;
			session.Save(new Address("Тестовый адрес доставки"));
			session.DeleteEach<BatchLine>();
			session.Save(new BatchLine(session.Query<Catalog>().First(), address) {
				Comment = "test comment",
				Properties = "Клубничный",
			});

			WpfTestHelper.WithWindow2(async w => {
				var model = new Batch();
				var view = Bind(model);
				w.Content = view;

				await view.WaitLoaded();

				var searchCheck = view.Descendants<CheckBox>().First(c => c.Name == "All");
				searchCheck.IsChecked = true;

				var grid = view.Descendants<DataGrid>().First(c => c.Name == "ReportLines");
				grid.CurrentItem = grid.Items[0];
				await view.WaitIdle();

				var col = DataGridHelper.FindColumn(grid, "Адрес заказа");
				Assert.AreEqual(col.Visibility, Visibility.Visible);
				var comment = view.Descendants<TextBox>().First(c => c.Name == "CurrentReportLine_Value_Comment");
				Assert.AreEqual("test comment", comment.Text);

				col = DataGridHelper.FindColumn(grid, "Кат.свойства");
				col.Visibility = Visibility.Visible;
				await view.WaitIdle();
				var cell = grid.Descendants<DataGridCell>().First(x => x.Column == col);
				Assert.AreEqual("Клубничный", cell.Descendants<TextBlock>().First().Text);
			});
		}
	}
}
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Orders;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture]
	public class Batch2Fixture : BaseViewFixture
	{
		[Test]
		public void Show_address()
		{
			restore = true;
			session.Save(new Address("Тестовый адрес доставки"));
			session.Save(new BatchLine(session.Query<Catalog>().First(), address));

			WpfTestHelper.WithWindow2(async w => {
				var model = new Batch2();
				var view = Bind(model);
				w.Content = view;

				await view.WaitLoaded();

				var searchCheck = view.Descendants<CheckBox>().First(c => c.Name == "All");
				searchCheck.IsChecked = true;

				var grid = view.Descendants<DataGrid>().First(c => c.Name == "BatchLines");
				var col = DataGridHelper.GetColumn(grid, "Адрес заказа");
				Assert.AreEqual(col.Visibility, Visibility.Visible);
			});
		}
	}
}
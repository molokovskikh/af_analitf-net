using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Orders;
using Caliburn.Micro;
using Common.NHibernate;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture]
	public class OrdersFixture : BaseViewFixture
	{
		[Test]
		public void Show_address_column()
		{
			restore = true;
			session.Save(new Address { Name = "Тестовый адрес доставки" });

			var model = new OrdersViewModel();
			UseWindow(model, async (w, view) => {
				var all = view.Descendants<CheckBox>().First(c => c.Name == "All");
				Assert.That(all.Visibility, Is.EqualTo(Visibility.Visible));
				var grid = view.Descendants<DataGrid>().First(c => c.Name == "Orders");
				var column = DataGridHelper.FindColumn(grid.Columns, "Адрес заказа");

				Assert.That(column.Visibility, Is.EqualTo(Visibility.Collapsed));
				model.AddressSelector.All.Value = true;
				Assert.That(column.Visibility, Is.EqualTo(Visibility.Visible));
			});
		}

		[Test]
		public void Show_action_buttons()
		{
			var model = new OrdersViewModel();
			UseWindow(model, async (w, view) => {
				var tabs = view.Descendants<TabControl>().First();
				tabs.SelectedItem = tabs.Items[1];
				var restore = view.Descendants<Button>().First(b => b.Name == "RestoreOrder");
				var reorder = view.Descendants<Button>().First(b => b.Name == "Reorder");
				var freeze = view.Descendants<Button>().First(b => b.Name == "Freeze");
				Assert.AreEqual(Visibility.Visible, restore.Visibility);
				Assert.AreEqual(Visibility.Visible, reorder.Visibility);
				Assert.AreEqual(Visibility.Collapsed, freeze.Visibility);
			});
		}

		[Test]
		public void Highlight_current_address()
		{
			restore = true;
			session.DeleteEach<SentOrder>();
			address = new Address { Name = "Тестовый адрес доставки" };
			session.Save(address);
			MakeSentOrder();

			var model = new OrdersViewModel();
			UseWindow(model, async (w, view) => {
				var all = view.Descendants<CheckBox>().First(c => c.Name == "All");
				Assert.That(all.Visibility, Is.EqualTo(Visibility.Visible));
				all.IsChecked = true;

				var tabs = view.Descendants<TabControl>().First();
				tabs.SelectedItem = tabs.Items[1];

				var grid = (DataGrid)((TabItem)tabs.Items[1]).Content;
				await grid.WaitLoaded();
				var column = DataGridHelper.FindColumn(grid.Columns, "Адрес заказа");

				var cell = grid.Descendants<DataGridCell>().First(x => x.Column == column);
				var text = cell.Descendants<TextBlock>().First();
				Assert.AreEqual(FontWeights.Bold, text.FontWeight);
			});
		}
	}
}
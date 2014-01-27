using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using NUnit.Framework;
using WpfHelper = AnalitF.Net.Client.Test.TestHelpers.WpfHelper;

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
			var view = Bind(model);

			var all = view.Descendants<CheckBox>().First(c => c.Name == "All");
			Assert.That(all.Visibility, Is.EqualTo(Visibility.Visible));
			var grid = view.Descendants<DataGrid>().First(c => c.Name == "Orders");
			var column = grid.Columns.First(c => c.Header.Equals("Адрес заказа"));

			Assert.That(column.Visibility, Is.EqualTo(Visibility.Collapsed));
			model.AddressSelector.All.Value = true;
			//биндинг почему то не работает
			((Client.Controls.DataGrid2)grid).ShowAddressColumn = true;
			Assert.That(column.Visibility, Is.EqualTo(Visibility.Visible));
		}

		[Test]
		public void Show_action_buttons()
		{
			WpfHelper.WithWindow(w => {
				var model = new OrdersViewModel();
				var view = Bind(model);
				w.Content = view;

				w.Loaded += (sender, args) => {
					var tabs = view.Descendants<TabControl>().First();
					tabs.SelectedItem = tabs.Items[1];
					var restore = view.Descendants<Button>().First(b => b.Name == "Restore");
					var reorder = view.Descendants<Button>().First(b => b.Name == "Reorder");
					var freeze = view.Descendants<Button>().First(b => b.Name == "Freeze");
					Assert.AreEqual(Visibility.Visible, restore.Visibility);
					Assert.AreEqual(Visibility.Visible, reorder.Visibility);
					Assert.AreEqual(Visibility.Collapsed, freeze.Visibility);
					WpfHelper.Shutdown(w);
				};
			});
		}
	}
}
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture, RequiresSTA]
	public class OrdersFixture : BaseViewFixture
	{
		[Test]
		public void Show_address_column()
		{
			Restore = true;

			session.Save(new Address { Name = "Тестовый адрес доставки" });

			var model = new OrdersViewModel();
			var view = Bind(model);

			var all = view.DeepChildren().OfType<CheckBox>().First(c => c.Name == "All");
			Assert.That(all.Visibility, Is.EqualTo(Visibility.Visible));
			var grid = view.DeepChildren().OfType<DataGrid>().First(c => c.Name == "Orders");
			var column = grid.Columns.First(c => c.Header.Equals("Адрес заказа"));

			Assert.That(column.Visibility, Is.EqualTo(Visibility.Collapsed));
			model.AddressSelector.All.Value = true;
			//биндинг почемуто не работает
			((Client.Controls.DataGrid)grid).ShowAddressColumn = true;
			Assert.That(column.Visibility, Is.EqualTo(Visibility.Visible));
		}
	}
}
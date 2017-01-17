using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Orders;
using NHibernate.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AnalitF.Net.Client.Test.Integration.Views
{
	[TestFixture]
	public class OrderDetailsFixture : BaseViewFixture
	{
		[Test]
		public void Check_sending_retail_info()
		{
			restore = true;

			var order = session.Query<Order>().FirstOrDefault();
			if (order == null)
			{
				var address = session.Query<Address>().FirstOrDefault();
				if (address == null)
				{
					address = new Address {Name = "Тестовый адрес доставки"};
					session.Save(address);
				}
				var price = session.Query<Price>().FirstOrDefault();
				if (price == null)
				{
					price = new Price("тест");
					session.Save(price);
				}
				var offer = session.Query<Offer>().FirstOrDefault(r => r.Price == price);
				if (offer == null)
				{
					offer = new Offer(price, 100);
					session.Save(offer);
				}
				order = new Order(address, offer);
				session.Save(order);
			}

			var model = new OrderDetailsViewModel(order);
			model.User.SendRetailMarkup = true;
			UseWindow(model, async (w, view) => {
				var grid = view.Descendants<DataGrid>().First(c => c.Name == "Lines");
				var column = DataGridHelper.FindColumn(grid.Columns, "Розничная наценка");
				Assert.IsNotNull(column);
			});

			user.SendRetailMarkup = false;
			model = new OrderDetailsViewModel(order);
			model.User.SendRetailMarkup = false;
			UseWindow(model, async (w, view) => {
				var grid = view.Descendants<DataGrid>().First(c => c.Name == "Lines");
				var column = DataGridHelper.FindColumn(grid.Columns, "Розничная наценка");
				Assert.IsNull(column);
			});
		}
	}
}

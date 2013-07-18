using System.Linq;
using System.Windows;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class OrderDetailsFixture : BaseFixture
	{
		[Test]
		public void Delete_line()
		{
			manager.DefaultResult = MessageBoxResult.Yes;
			var order = MakeOrder();

			var model = Init(new OrderDetailsViewModel(order));
			model.CurrentLine = model.Lines.First();
			model.Delete();
			Assert.That(model.Lines.Count, Is.EqualTo(0));

			Close(model);

			session.Clear();
			Assert.IsNull(session.Get<Order>(order.Id));
		}

		[Test]
		public void Close_on_last_delete()
		{
			manager.DefaultResult = MessageBoxResult.Yes;
			MakeOrder();

			shell.ShowOrders();
			var orders = (OrdersViewModel)shell.ActiveItem;
			orders.CurrentOrder = orders.Orders.First();
			orders.EnterOrder();

			var lines = (OrderDetailsViewModel)shell.ActiveItem;
			lines.CurrentLine = lines.Lines.First();
			lines.Delete();
			Assert.That(shell.ActiveItem, Is.InstanceOf<OrdersViewModel>());

			//не реализовано
			//Assert.That(orders.Orders.Count, Is.EqualTo(0));
		}

		[Test]
		public void Show_price()
		{
			var order = MakeOrder();
			var model = Init(new OrderDetailsViewModel(order));
			Assert.That(model.CanShowPrice, Is.True);
			Assert.That(model.ShowPriceVisible, Is.True);
			model.ShowPrice();

			Assert.That(shell.ActiveItem, Is.InstanceOf<PriceOfferViewModel>());
		}
	}
}
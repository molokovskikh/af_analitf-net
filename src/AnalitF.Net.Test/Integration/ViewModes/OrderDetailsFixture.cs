﻿using System.Linq;
using System.Windows;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class OrderDetailsFixture : ViewModelFixture
	{
		[Test]
		public void Edit_order()
		{
			var order = MakeOrder();
			var model = Init(new OrderDetailsViewModel(order));

			model.CurrentLine.Value = model.Lines.Value.First();
			((OrderLine)model.CurrentLine.Value).Count = 1;
			model.OfferUpdated();
			Close(model);
		}

		[Test]
		public void Delete_line()
		{
			var order = MakeOrder();
			var model = Init(new OrderDetailsViewModel(order));

			model.CurrentLine.Value = model.Lines.Value.First();
			model.Delete();
			Assert.That(model.Lines.Value.Count, Is.EqualTo(0));

			Close(model);

			session.Clear();
			Assert.IsNull(session.Get<Order>(order.Id));
		}

		[Test]
		public void Close_on_last_delete()
		{
			MakeOrder();

			shell.ShowOrders();
			var orders = (OrdersViewModel)shell.ActiveItem;
			var order = orders.Orders.First();
			orders.CurrentOrder = order;
			orders.EnterOrder();

			var lines = (OrderDetailsViewModel)shell.ActiveItem;
			lines.CurrentLine.Value = lines.Lines.Value.First();
			lines.Delete();

			Assert.That(shell.ActiveItem, Is.InstanceOf<OrdersViewModel>());
			Assert.That(orders.Orders.Select(o => o.Id), Is.Not.Contains(order));
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

		[Test]
		public void Filter_on_warning()
		{
			var fixture = Fixture<CorrectOrder>();
			var order = fixture.Order;

			shell.ShowOrders();
			var orders = (OrdersViewModel)shell.ActiveItem;
			orders.CurrentOrder = orders.Orders.First(o => o.Id == order.Id);
			orders.EnterOrder();

			var lines = (OrderDetailsViewModel)shell.ActiveItem;
			Assert.AreEqual(2, lines.Lines.Value.Count);
			lines.OnlyWarning.Value = true;
			Assert.AreEqual(1, lines.Lines.Value.Count);
			lines.CurrentLine.Value = lines.Lines.Value.First();
			lines.Delete();
			Assert.AreEqual(0, lines.Lines.Value.Count);
			Assert.IsInstanceOf<OrderDetailsViewModel>(shell.ActiveItem);
		}
	}
}
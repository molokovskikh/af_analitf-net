using System;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.ViewModels.Orders;
using Common.NHibernate;
using Common.Tools;
using NUnit.Framework;
using ReactiveUI;
using System.Windows;
using ReactiveUI.Testing;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class OrderDetailsFixture : ViewModelFixture
	{
		[Test]
		public void Edit_order()
		{
			var order = MakeOrder();
			var model = Open(new OrderDetailsViewModel(order));

			model.CurrentLine.Value = model.Lines.Value.First();
			((OrderLine)model.CurrentLine.Value).Count = 1;
			model.OfferUpdated();
			Close(model);
		}

		[Test]
		public void Delete_line()
		{
			var order = MakeOrder();
			var model = Open(new OrderDetailsViewModel(order));

			model.CurrentLine.Value = model.Lines.Value.First();
			model.Delete();
			Assert.That(model.Lines.Value.Count, Is.EqualTo(0));

			Close(model);

			session.Clear();
			Assert.IsNull(session.Get<Order>(order.Id));
		}

		[Test]
		public void Delete_line_confirm()
		{
			var order = MakeOrder();
			var model = Open(new OrderDetailsViewModel(order));

			manager.DefaultQuestsionResult = MessageBoxResult.No;
			model.CurrentLine.Value = model.Lines.Value.First();
			model.Delete();
			Assert.AreEqual(1, model.Lines.Value.Count);
			manager.DefaultQuestsionResult = MessageBoxResult.Yes;
			model.CurrentLine.Value = model.Lines.Value.First();
			model.Delete();
			Assert.AreEqual(0, model.Lines.Value.Count);
		}

		[Test]
		public void Close_on_last_delete()
		{
			MakeOrder();

			shell.ShowOrders();
			scheduler.Start();
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
			var model = Open(new OrderDetailsViewModel(order));

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
			scheduler.Start();
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

		[Test]
		public void View_mixed_cost()
		{
			restore = true;
			user.IsDelayOfPaymentEnabled = true;

			var order = MakeOrder();
			order.Price.CostFactor = 1.5m;
			order.Price.VitallyImportantCostFactor = 1.5m;
			var model = Open(new OrderDetailsViewModel(order));

			var cost = Math.Round(1.5m * order.Lines[0].Cost, 2);
			var orderLine = model.Lines.Value[0];
			Assert.AreEqual(cost, orderLine.MixedSum);
			Assert.AreEqual(cost, orderLine.ResultCost);
		}

		[Test]
		public void Notify_on_order_reload()
		{
			session.DeleteEach<Order>();
			var order = MakeOrder();
			var cost = order.Lines[0].Cost;
			var model = Open(new OrderDetailsViewModel(order));
			var events = model.ObservableForProperty(x => x.Order.Value.Sum).Collect();
			model.CurrentLine.Value = model.Lines.Value.First();
			scheduler.AdvanceByMs(500);
			model.EnterLine();
			var offers = (CatalogOfferViewModel)shell.ActiveItem;
			scheduler.Start();
			offers.CurrentOffer.Value.OrderCount = 2;
			offers.OfferUpdated();
			offers.OfferCommitted();
			offers.NavigateBackward();
			((OrderLine)model.CurrentLine.Value).Count = 1;
			model.OfferUpdated();
			model.OfferCommitted();
			Assert.AreEqual(2, events.Count, $"cost = {cost}, {events.Implode(x => x.Value)}");
			Assert.AreEqual(cost * 2, events[0].Value);
			Assert.AreEqual(cost, events[1].Value);
		}
	}
}
using System;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class OrdersFixture : BaseFixture
	{
		private Lazy<OrdersViewModel> lazyModel;
		private OrdersViewModel model
		{
			get { return lazyModel.Value; }
		}

		[SetUp]
		public void Setup()
		{
			lazyModel = new Lazy<OrdersViewModel>(() => {
				session.Flush();
				return Init(new OrdersViewModel());
			});
		}

		[Test]
		public void Load_order_on_open_tab()
		{
			var view = Init(new OrdersViewModel());
			Assert.That(view.SentOrders, Is.Null);
			view.IsSentSelected = true;
			view.IsCurrentSelected = false;
			Assert.That(view.SentOrders, Is.Not.Null);
		}

		[Test]
		public void Print()
		{
			MakeOrder(session.Query<Offer>().First());

			model.CurrentOrder = model.Orders.First();
			Assert.That(model.CanPrint, Is.True);
			var doc = model.Print().Doc;
			Assert.That(doc, Is.Not.Null);
		}

		[Test]
		public void Disable_send_order_button()
		{
			session.DeleteEach<Order>();
			MakeOrder(session.Query<Offer>().First());

			model.Orders[0].Send = false;

			shell.NotifyOfPropertyChange("CurrentAddress");
			Assert.That(shell.CanSendOrders, Is.True);

			testScheduler.AdvanceByMs(5000);
			Assert.That(shell.CanSendOrders, Is.False);
		}

		[Test]
		public void Freeze_order()
		{
			session.DeleteEach<Order>();
			MakeOrder(session.Query<Offer>().First());

			model.CurrentOrder = model.Orders.First();

			shell.NotifyOfPropertyChange("CurrentAddress");
			Assert.That(shell.CanSendOrders, Is.True);
			Assert.That(shell.Stat.Value.OrdersCount, Is.EqualTo(1));

			Assert.True(model.CanFreeze);
			model.Freeze();

			testScheduler.AdvanceByMs(1000);
			Assert.That(shell.CanSendOrders, Is.False);
			Assert.That(shell.Stat.Value.OrdersCount, Is.EqualTo(0));
		}

		[Test]
		public void Unfreeze_order()
		{
			session.DeleteEach<Order>();
			var order = MakeOrder(session.Query<Offer>().First());

			model.CurrentOrder = model.Orders.First();
			Assert.That(model.CanFreeze, Is.True);
			model.Freeze();
			Assert.That(model.CanUnfreeze);
			model.Unfreeze();
			Assert.That(model.Orders.Select(o => o.Id).ToArray(), Is.Not.Contains(order.Id));
			Assert.That(model.Orders.Count, Is.EqualTo(1));
		}

		[Test]
		public void Reorder()
		{
			Restore = true;

			session.DeleteEach<Order>();
			var offer = session.Query<Offer>().First();
			var price = session.Query<Price>().First(p => p.Id.PriceId != offer.Price.Id.PriceId);
			var newOffer = new Offer(price, offer, offer.Cost + 50);
			newOffer.Id.OfferId += (ulong)Generator.Random(int.MaxValue).First();
			session.Save(newOffer);

			var order = MakeOrder(offer);
			MakeOrder(newOffer);

			model.CurrentOrder = model.Orders.First(o => o.Id == order.Id);
			Assert.That(model.CanReorder, Is.True);
			model.Reorder();
			Assert.That(model.Orders.Select(o => o.Id).ToArray(), Is.Not.Contains(order.Id));
			Assert.That(model.Orders.Count, Is.EqualTo(1));
			Assert.That(model.Orders[0].Lines[0].Count, Is.EqualTo(2));
		}
	}
}
using System;
using System.Linq;
using System.Windows;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
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
			manager.DefaultResult = MessageBoxResult.Yes;
			lazyModel = new Lazy<OrdersViewModel>(() => {
				session.Flush();
				return Init(new OrdersViewModel());
			});
		}

		[TearDown]
		public void Teardown()
		{
			if (lazyModel.IsValueCreated)
				ScreenExtensions.TryDeactivate(model, true);
		}

		[Test]
		public void Delete_order()
		{
			session.DeleteEach<Order>();

			var order = MakeOrder();
			model.IsCurrentSelected = true;
			model.CurrentOrder = model.Orders.First();
			Assert.That(model.CanDelete, Is.True);
			model.Delete();
			testScheduler.AdvanceByMs(5000);
			ScreenExtensions.TryDeactivate(model, true);

			session.Clear();
			Assert.Null(session.Get<Order>(order.Id));
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
			MakeOrder(MakeReordarable(offer));
			var order = MakeOrder(offer);

			model.CurrentOrder = model.Orders.First(o => o.Id == order.Id);
			Assert.That(model.CanReorder, Is.True);
			model.Reorder();
			Assert.That(model.Orders.Select(o => o.Id).ToArray(), Is.Not.Contains(order.Id));
			Assert.That(model.Orders.Count, Is.EqualTo(1));
			Assert.That(model.Orders[0].Lines[0].Count, Is.EqualTo(2));
		}

		[Test]
		public void Reorder_sent_order()
		{
			Restore = true;

			session.DeleteEach<Order>();
			session.DeleteEach<SentOrder>();
			var offer = session.Query<Offer>().First();
			MakeSentOrder(MakeReordarable(offer));
			MakeOrder(offer);

			SelectSent();

			Assert.That(model.CanReorder, Is.True);
			model.Reorder();
			Assert.That(model.SentOrders.Count, Is.EqualTo(1));
			Assert.That(model.Orders.Count, Is.EqualTo(1));
			Assert.That(model.Orders[0].Lines.Count, Is.EqualTo(2));
		}

		[Test]
		public void Do_not_reorder_on_same_price()
		{
			session.DeleteEach<Order>();
			session.DeleteEach<SentOrder>();
			MakeSentOrder();
			MakeOrder();
			SelectSent();

			var result = model.Reorder();
			var text = ((TextViewModel)((DialogResult)result).Model).Text;
			Assert.That(text, Is.StringContaining("предложение отсутствует"));
			Assert.That(model.Orders.Count, Is.EqualTo(1));
			Assert.That(model.Orders[0].Lines.Count, Is.EqualTo(1));
			Assert.That(model.Orders[0].Lines[0].Count, Is.EqualTo(1));
		}

		[Test]
		public void Delete_sent_order()
		{
			session.DeleteEach<SentOrder>();
			var order = MakeSentOrder();

			model.IsCurrentSelected = false;
			model.IsSentSelected = true;
			model.CurrentSentOrder = model.SentOrders.First();
			Assert.That(model.CanDelete, Is.True);
			model.Delete();
			Assert.That(model.SentOrders, Is.Empty);

			session.Clear();
			Assert.That(session.Get<SentOrder>(order.Id), Is.Null);
		}

		[Test]
		public void Restore_order()
		{
			PrepareSent();

			Assert.That(model.CanRestore, Is.True);
			model.Restore();
			Assert.That(model.SentOrders.Count, Is.EqualTo(1));
			model.IsCurrentSelected = true;
			model.IsSentSelected = false;
			Assert.That(model.Orders.Count, Is.EqualTo(1));
		}

		[Test]
		public void Update_personal_comment()
		{
			var order = PrepareSent();

			model.CurrentSentOrder.PersonalComment = "тестовый комментарий";
			session.Refresh(order);
			Assert.AreEqual(order.PersonalComment, "тестовый комментарий");
		}

		[Test]
		public void Select_all_orders()
		{
			session.DeleteEach<Order>();

			Restore = true;
			var newAddress = new Address { Name = "Тестовый адрес доставки" };
			session.Save(newAddress);
			MakeOrder();
			shell.CurrentAddress = newAddress;

			Assert.That(model.Orders.Count, Is.EqualTo(0));
			model.AddressSelector.All.Value = true;
			Assert.That(model.Orders.Count, Is.EqualTo(1));
		}

		private Offer MakeReordarable(Offer offer)
		{
			var price = session.Query<Price>().First(p => p.Id.PriceId != offer.Price.Id.PriceId);
			var newOffer = new Offer(price, offer, offer.Cost + 50);
			newOffer.Id.OfferId += (ulong)Generator.Random(int.MaxValue).First();
			session.Save(newOffer);

			return newOffer;
		}

		private SentOrder PrepareSent()
		{
			session.DeleteEach<Order>();
			session.DeleteEach<SentOrder>();
			var order = MakeSentOrder();

			SelectSent();
			return order;
		}

		private void SelectSent()
		{
			model.IsCurrentSelected = false;
			model.IsSentSelected = true;
			model.CurrentSentOrder = model.SentOrders.First();
		}

		//[Test]
		//public void Move()
		//{
		//	Restore = true;

		//	session.DeleteEach<Order>();
		//	MakeOrder();

		//	model.CurrentOrder = model.Orders.First();
		//	Assert.NotNull(model.CurrentMoveAddress);
		//	Assert.True(model.CanMove);
		//	model.Move();
		//	Assert.That(model.CurrentOrder.Address.Id, Is.Not.EqualTo(address.Id));
		//}
	}
}
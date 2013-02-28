﻿using System;
using System.Linq;
using System.Windows;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class OrderLinesFixture : BaseFixture
	{
		Lazy<OrderLinesViewModel> lazyModel;

		private OrderLinesViewModel model
		{
			get { return lazyModel.Value; }
		}

		[SetUp]
		public void Setup()
		{
			session.DeleteEach<Order>();

			lazyModel = new Lazy<OrderLinesViewModel>(() => {
				session.Flush();
				return Init(new OrderLinesViewModel());
			});
		}

		[Test]
		public void Delete_order_line()
		{
			var offer = session.Query<Offer>().First();
			MakeOrder(offer);

			manager.DefaultResult = MessageBoxResult.Yes;
			Assert.That(model.Lines.Count, Is.EqualTo(1));
			model.CurrentLine = model.Lines.First(l => l.Id == offer.OrderLine.Id);
			model.Delete();
			Assert.That(model.Lines.Count, Is.EqualTo(0));
		}

		[Test]
		public void Load_sent_orders()
		{
			model.IsSentSelected = true;
			model.IsCurrentSelected = false;

			Assert.That(model.SentLines, Is.Not.Null);
		}

		[Test]
		public void Filter_by_address()
		{
			Restore = true;
			session.DeleteEach(session.Query<Address>().Skip(1));

			var newAddress = new Address("Тестовый адрес доставки");
			session.Save(newAddress);
			var offer = session.Query<Offer>().First();
			MakeOrder(offer);
			MakeOrder(offer, newAddress);

			model.AllOrders.Value = true;
			Assert.That(model.Lines.Count, Is.EqualTo(2));
			model.Addresses[1].IsSelected = false;
			Assert.That(model.Lines.Count, Is.EqualTo(2));
			testScheduler.AdvanceByMs(1000);
			Assert.That(model.Lines.Count, Is.EqualTo(1));
		}

		[Test]
		public void Show_catalog()
		{
			MakeOrder(session.Query<Offer>().First());

			model.CurrentLine = model.Lines.First();
			Assert.That(model.CanShowCatalog, Is.True);
			model.ShowCatalog();

			Assert.That(shell.ActiveItem, Is.InstanceOf<CatalogOfferViewModel>());
		}

		[Test]
		public void Print()
		{
			Assert.That(model.CanPrint, Is.True);
			var doc = model.Print().Doc;
			Assert.That(doc, Is.Not.Null);
		}

		[Test]
		public void Delete_line_on_edit()
		{
			var order = MakeOrder(session.Query<Offer>().First());

			model.CurrentLine = model.Lines.FirstOrDefault();
			model.CurrentLine.Count = 0;
			model.OfferUpdated();
			model.OfferCommitted();
			Assert.That(model.Lines.Count, Is.EqualTo(0));
			Assert.That(model.Sum, Is.EqualTo(0));

			ScreenExtensions.TryDeactivate(model, true);

			session.Clear();
			Assert.That(session.Get<Order>(order.Id), Is.Null);
			Assert.That(session.Get<OrderLine>(order.Lines[0].Id), Is.Null);
		}

		[Test]
		public void Update_stat_on_delete()
		{
			MakeOrder(session.Query<Offer>().First());
			model.CurrentLine = model.Lines.FirstOrDefault();

			shell.NotifyOfPropertyChange("CurrentAddress");
			Assert.That(shell.Stat.Value.OrdersCount, Is.EqualTo(1));
			model.CurrentLine.Count = 0;
			model.OfferUpdated();
			testScheduler.AdvanceByMs(1000);
			Assert.That(shell.Stat.Value.OrdersCount, Is.EqualTo(0));
		}

		[Test]
		public void Update_stat()
		{
			MakeOrder(session.Query<Offer>().First());
			model.CurrentLine = model.Lines.FirstOrDefault();
			model.CurrentLine.Count = 100;
			model.OfferUpdated();
			model.OfferCommitted();
			testScheduler.AdvanceByMs(1000);
			Assert.That(shell.Stat.Value.Sum, Is.EqualTo(model.CurrentLine.Sum));
		}
	}
}
using System;
using System.Linq;
using System.Windows;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;
using Common.NHibernate;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class OrderLinesFixture : ViewModelFixture
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

			Assert.That(model.Lines.Value.Count, Is.EqualTo(1));
			model.CurrentLine.Value = model.Lines.Value.First(l => l.Id == offer.OrderLine.Id);
			model.Delete();
			Assert.That(model.Lines.Value.Count, Is.EqualTo(0));
		}

		[Test]
		public void Load_sent_orders()
		{
			model.IsSentSelected.Value = true;
			model.IsCurrentSelected.Value = false;

			Assert.That(model.SentLines, Is.Not.Null);
		}

		[Test]
		public void Filter_by_address()
		{
			restore = true;
			session.DeleteEach(session.Query<Address>().Skip(1));

			var newAddress = new Address("Тестовый адрес доставки");
			session.Save(newAddress);
			var offer = session.Query<Offer>().First();
			MakeOrder(offer);
			MakeOrder(offer, newAddress);

			model.AddressSelector.All.Value = true;
			Assert.That(model.Lines.Value.Count, Is.EqualTo(2));
			model.AddressSelector.Addresses[1].IsSelected = false;
			Assert.That(model.Lines.Value.Count, Is.EqualTo(2));
			testScheduler.AdvanceByMs(1000);
			Assert.That(model.Lines.Value.Count, Is.EqualTo(1));
		}

		[Test]
		public void Show_catalog()
		{
			MakeOrder(session.Query<Offer>().First());

			model.CurrentLine.Value = model.Lines.Value.First();
			Assert.That(model.ProductInfo.CanShowCatalog, Is.True);
			model.ProductInfo.ShowCatalog();

			Assert.That(shell.ActiveItem, Is.InstanceOf<CatalogOfferViewModel>());
		}

		[Test]
		public void Print()
		{
			Assert.That(model.CanPrint, Is.True);
			var doc = model.Print().Paginator;
			Assert.That(doc, Is.Not.Null);
		}

		[Test]
		public void Delete_line_on_edit()
		{
			var order = MakeOrder(session.Query<Offer>().First());

			model.CurrentLine.Value = model.Lines.Value.FirstOrDefault();
			model.CurrentLine.Value.Count = 0;
			model.OfferUpdated();
			model.OfferCommitted();
			testScheduler.AdvanceByMs(5000);
			Assert.That(model.Lines.Value.Count, Is.EqualTo(0));
			Assert.That(model.Sum.Value, Is.EqualTo(0));

			Close(model);

			session.Clear();
			Assert.That(session.Get<Order>(order.Id), Is.Null);
			Assert.That(session.Get<OrderLine>(order.Lines[0].Id), Is.Null);
		}

		[Test]
		public void Update_stat_on_delete()
		{
			MakeOrder(session.Query<Offer>().First());
			model.CurrentLine.Value = model.Lines.Value.FirstOrDefault();

			shell.NotifyOfPropertyChange("CurrentAddress");
			Assert.That(shell.Stat.Value.OrdersCount, Is.EqualTo(1));
			model.CurrentLine.Value.Count = 0;
			model.OfferUpdated();
			testScheduler.AdvanceByMs(1000);
			Assert.That(shell.Stat.Value.OrdersCount, Is.EqualTo(0));
		}

		[Test]
		public void Update_stat()
		{
			MakeOrder(session.Query<Offer>().First());
			model.CurrentLine.Value = model.Lines.Value.FirstOrDefault();
			model.CurrentLine.Value.Count = 100;
			model.OfferUpdated();
			model.OfferCommitted();
			testScheduler.AdvanceByMs(1000);
			Assert.That(shell.Stat.Value.Sum, Is.EqualTo(model.CurrentLine.Value.Sum));
		}

		[Test]
		public void Update_sum_on_type_change()
		{
			session.DeleteEach<SentOrderLine>();
			MakeOrder();

			Assert.That(model.Sum.Value, Is.GreaterThan(0));
			model.IsCurrentSelected.Value = false;
			model.IsSentSelected.Value = true;

			Assert.That(model.Sum.Value, Is.EqualTo(0));

			model.IsSentSelected.Value = false;
			model.IsCurrentSelected.Value = true;
			Assert.That(model.Sum.Value, Is.GreaterThan(0));
		}
	}
}
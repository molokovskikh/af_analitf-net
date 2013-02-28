using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using NHibernate.Linq;
using NUnit.Framework;

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
	}
}
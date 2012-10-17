using System.Linq;
using System.Windows;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class OrderLinesFixture : BaseFixture
	{
		[SetUp]
		public void Setup()
		{
			var orders = session.Query<Order>().ToList();
			orders.Each(o => o.Price.Order = null);
			orders.SelectMany(o => o.Lines)
				.Select(l => session.Load<Offer>(l.OfferId))
				.Each(o => o.OrderLine = null);
			orders.Each(o => session.Delete(o));
			session.Flush();
			session.Clear();
		}

		[Test]
		public void Show()
		{
			var model = Init(new OrderLinesViewModel());
		}

		[Test]
		public void Delete_order_line()
		{
			var model = Init(new OrderLinesViewModel());
			manager.DefaultResult = MessageBoxResult.Yes;

			var offer = session.Query<Offer>().First();
			offer.OrderCount = 1;
			offer.UpdateOrderLine();
			session.Save(offer.Price.Order);
			session.Flush();
			session.Clear();

			model = Init(new OrderLinesViewModel());
			Assert.That(model.Lines.Count, Is.EqualTo(1));
			model.CurrentLine = model.Lines.First(l => l.Id == offer.OrderLine.Id);
			model.Delete();
			Assert.That(model.Lines.Count, Is.EqualTo(0));
		}
	}
}
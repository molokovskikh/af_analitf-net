using System.Linq;
using System.Windows;
using AnalitF.Net.Client.Helpers;
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
			session.DeleteEach<Order>();
			session.Flush();
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
			offer.UpdateOrderLine(address, settings);
			session.Save(offer.Price.Order);
			session.Flush();
			session.Clear();

			model = Init(new OrderLinesViewModel());
			Assert.That(model.Lines.Count, Is.EqualTo(1));
			model.CurrentLine = model.Lines.First(l => l.Id == offer.OrderLine.Id);
			model.Delete();
			Assert.That(model.Lines.Count, Is.EqualTo(0));
		}

		[Test]
		public void Load_sent_orders()
		{
			var model = Init(new OrderLinesViewModel());

			model.IsSentSelected = true;
			model.IsCurrentSelected = false;

			Assert.That(model.SentLines, Is.Not.Null);
		}
	}
}
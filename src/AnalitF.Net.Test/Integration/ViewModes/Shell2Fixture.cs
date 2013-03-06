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

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class Shell2Fixture : BaseFixture
	{
		[Test]
		public void Update_order_stat_on_order_change()
		{
			session.DeleteEach<Order>();
			session.Flush();

			shell.ShowPrice();
			var prices = (PriceViewModel)shell.ActiveItem;
			prices.CurrentPrice = prices.Prices.First(p => p.PositionCount > 0);
			prices.EnterPrice();
			var offers = (PriceOfferViewModel)shell.ActiveItem;
			offers.CurrentOffer.OrderCount = 1;
			offers.OfferUpdated();
			offers.OfferCommitted();
			offers.NavigateBackward();

			testScheduler.AdvanceByMs(1000);

			var stat = shell.Stat.Value;
			Assert.That(stat.OrdersCount, Is.EqualTo(1));
			Assert.That(stat.OrderLinesCount, Is.EqualTo(1));
			Assert.That(stat.Sum, Is.GreaterThan(0));
		}
	}
}
using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Offers;
using Common.NHibernate;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class PriceFixture : ViewModelFixture<PriceViewModel>
	{
		[Test]
		public void Load_order()
		{
			var offer = session.Query<Offer>().First();
			MakeOrder(offer);

			Assert.That(model.Prices.First(p => p.Id == offer.Price.Id).Order, Is.Not.Null);
			Assert.That(model.Prices[0].MinOrderSum, Is.Not.Null);
		}

		[Test]
		public void Auto_open_single_price()
		{
			restore = true;
			var price = session.Query<Price>().First(p => p.PositionCount > 0);
			session.DeleteEach(session.Query<Price>().ToList().Where(p => p.Id != price.Id));
			session.Flush();

			shell.ShowPrice();
			Assert.That(shell.ActiveItem, Is.InstanceOf<PriceOfferViewModel>());
			var view = (PriceOfferViewModel)shell.ActiveItem;
			view.NavigateBackward();
			Assert.That(shell.ActiveItem, Is.InstanceOf<PriceViewModel>());
		}

		[Test]
		public void Order_history()
		{
			var offer = session.Query<Offer>().First();
			MakeSentOrder(offer);

			var price = model.Prices.First(p => p.Id == offer.Price.Id);
			Assert.That(price.WeeklyOrderSum, Is.GreaterThan(0));
			Assert.That(price.MonthlyOrderSum, Is.GreaterThan(0));
		}

		[Test]
		public void Save_current_price()
		{
			var price = model.Prices[1];
			model.CurrentPrice.Value = price;
			Close(model);

			var model2 = Init(new PriceViewModel());
			Assert.That(model2.CurrentPrice.Value.Id, Is.EqualTo(price.Id));
			var persistedPrice = model2.Prices.FirstOrDefault(p => p.Id == model.CurrentPrice.Value.Id);
			Assert.That(model2.CurrentPrice.Value, Is.EqualTo(persistedPrice));
		}

		[Test]
		public void Reload_order_info_on_activate()
		{
			session.DeleteEach<Order>();

			Activate(model);
			Deactivate(model);

			var offer = session.Query<Offer>().First();
			MakeOrder(offer);
			bus.SendMessage("Changed", "db");

			Activate(model);

			var price = model.Prices.First(p => p.Id == offer.Price.Id);
			Assert.That(price.Order, Is.Not.Null);
		}
	}
}
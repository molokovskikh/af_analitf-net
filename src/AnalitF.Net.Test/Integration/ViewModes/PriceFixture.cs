using System;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class PriceFixture : BaseFixture
	{
		[Test]
		public void Load_order()
		{
			var offer = session.Query<Offer>().First();
			MakeOrder(offer);

			var model = Init(new PriceViewModel());
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

			var model = Init(new PriceViewModel());
			var price = model.Prices.First(p => p.Id == offer.Price.Id);
			Assert.That(price.WeeklyOrderSum, Is.GreaterThan(0));
			Assert.That(price.MonthlyOrderSum, Is.GreaterThan(0));
		}

		[Test]
		public void Save_current_price()
		{
			var model = Init(new PriceViewModel());
			var price = model.Prices[1];
			model.CurrentPrice.Value = price;
			Close(model);

			model = Init(new PriceViewModel());
			Assert.That(model.CurrentPrice.Value.Id, Is.EqualTo(price.Id));
			var persistedPrice = model.Prices.FirstOrDefault(p => p.Id == model.CurrentPrice.Value.Id);
			Assert.That(model.CurrentPrice.Value, Is.EqualTo(persistedPrice));
		}

		[Test]
		public void Reload_order_info_on_activate()
		{
			session.DeleteEach<Order>();
			session.Flush();

			var model = Init(new PriceViewModel());
			Activate(model);
			Deactivate(model);

			var offer = session.Query<Offer>().First();
			MakeOrder(offer);
			RxApp.MessageBus.SendMessage("DbChanged");

			Activate(model);

			var price = model.Prices.First(p => p.Id == offer.Price.Id);
			Assert.That(price.Order, Is.Not.Null);
		}
	}
}
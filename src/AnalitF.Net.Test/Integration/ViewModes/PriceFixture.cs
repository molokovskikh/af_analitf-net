using System;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class PriceFixture : BaseFixture
	{
		private bool Restore;

		[SetUp]
		public void Setup()
		{
			Restore = false;
		}

		[TearDown]
		public void TearDown()
		{
			if (Restore) {
				SetupFixture.RestoreData(session);
			}
		}

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
			Restore = true;
			session.DeleteEach(session.Query<Price>().Skip(1));
			session.Flush();

			shell.ShowPrice();
			Assert.That(shell.ActiveItem, Is.InstanceOf<PriceOfferViewModel>());
			var view = (PriceOfferViewModel)shell.ActiveItem;
			view.NavigateBackward();
			Assert.That(shell.ActiveItem, Is.InstanceOf<PriceViewModel>());
		}

		//[Test]
		//public void Order_history()
		//{
		//	MakeSentOrder(session.Query<Offer>().First());

		//	var model = Init(new PriceViewModel());
		//	Assert.That(model.Prices[0].WeeklyOrderSum, Is.GreaterThan(0));
		//	Assert.That(model.Prices[0].MonthlyOrderSum, Is.GreaterThan(0));
		//}

		//[Test]
		//public void Save_current_price()
		//{
		//	var model = Init(new PriceViewModel());
		//	var price = model.Prices[1];
		//	model.CurrentPrice = price;
		//	model = Init(new PriceViewModel());
		//	Assert.That(model.CurrentPrice.Id, Is.EqualTo(price.Id));
		//}
	}
}
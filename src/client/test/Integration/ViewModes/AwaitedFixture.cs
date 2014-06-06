﻿using System;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Offers;
using Common.NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class AwaitedFixture : ViewModelFixture<Awaited>
	{
		[Test]
		public void Delete_awaited_on_order()
		{
			session.DeleteEach<AwaitedItem>();

			var offer = session.Query<Offer>().First();
			session.Save(new AwaitedItem(session.Load<Catalog>(offer.CatalogId)));

			Assert.AreEqual(1, model.Items.Value.Count);
			model.CurrentItem.Value = model.Items.Value.First();
			testScheduler.AdvanceByMs(500);
			Assert.That(model.Offers.Value.Count, Is.GreaterThan(0));

			model.CurrentOffer.Value = model.Offers.Value.FirstOrDefault();
			model.CurrentOffer.Value.OrderCount = 1;
			model.OfferUpdated();
			model.OfferCommitted();
			Close(model);

			Assert.AreEqual(0, session.Query<AwaitedItem>().Count());
		}

		[Test]
		public void Calculate_have_offer_on_add_item()
		{
			session.DeleteEach<AwaitedItem>();

			var offer = session.Query<Offer>().First();
			var catalog = session.Load<Catalog>(offer.CatalogId);
			var seq = model.Add().GetEnumerator();
			seq.MoveNext();
			var addAwaited = ((AddAwaited)((DialogResult)seq.Current).Model);
			addAwaited.Item.Catalog = catalog;
			addAwaited.OK();
			seq.MoveNext();

			var items = session.Query<AwaitedItem>().ToList();
			Assert.AreEqual(1, items.Count);
			Assert.AreEqual(items[0].Catalog, catalog);
		}
	}
}
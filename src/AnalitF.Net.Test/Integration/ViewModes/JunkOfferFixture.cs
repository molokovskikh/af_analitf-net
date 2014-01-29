using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Common.NHibernate;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	public class JunkOfferFixture : ViewModelFixture<JunkOfferViewModel>
	{
		[Test]
		public void Update_orders()
		{
			session.DeleteEach<Order>();

			shell.Navigate(model);
			model.CurrentOffer = model.Offers.Value.First();
			model.CurrentOffer.OrderCount = 1;
			model.OfferUpdated();
			model.OfferCommitted();

			model.ShowCatalog();

			var catalog = (CatalogOfferViewModel)shell.ActiveItem;
			catalog.CurrentOffer = catalog.Offers.Value.First(c => c.Id == model.CurrentOffer.Id);
			catalog.CurrentOffer.OrderCount = 0;
			catalog.OfferCommitted();
			catalog.OfferUpdated();
			catalog.TryClose();

			Assert.IsNull(model.CurrentOffer.OrderCount);
		}
	}
}
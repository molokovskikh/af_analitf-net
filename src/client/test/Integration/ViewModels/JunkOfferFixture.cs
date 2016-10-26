using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Offers;
using Common.NHibernate;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	public class JunkOfferFixture : ViewModelFixture<JunkOfferViewModel>
	{
		[Test]
		public void Update_orders()
		{
			session.DeleteEach<Order>();

			shell.Navigate(model);
			scheduler.Start();
			model.CurrentOffer.Value = model.Offers.Value.First();
			model.CurrentOffer.Value.OrderCount = 1;
			model.OfferUpdated();
			model.OfferCommitted();

			model.ShowCatalog();

			var catalog = (CatalogOfferViewModel)shell.ActiveItem;
			scheduler.Start();
			catalog.CurrentOffer.Value = catalog.Offers.Value.First(c => c.Id == model.CurrentOffer.Value.Id);
			catalog.CurrentOffer.Value.OrderCount = 0;
			catalog.OfferCommitted();
			catalog.OfferUpdated();
			catalog.TryClose();

			Assert.IsNull(model.CurrentOffer.Value.OrderCount);
		}
	}
}
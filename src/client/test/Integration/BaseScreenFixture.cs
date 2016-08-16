using System.Linq;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Offers;
using Caliburn.Micro;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration
{
	[TestFixture]
	public class BaseScreenFixture : ViewModelFixture
	{
		[Test]
		public void Open_new_item()
		{
			shell.ShowCatalog();
			var catalog = ((CatalogViewModel)shell.ActiveItem);
			var names = (CatalogNameViewModel)catalog.ActiveItem;
			scheduler.Start();
			names.CurrentCatalog = names.Catalogs.Value[0];
			names.EnterCatalog();
			Assert.That(shell.ActiveItem, Is.InstanceOf<CatalogOfferViewModel>());

			shell.ShowCatalog();
			Assert.That(shell.NavigationStack.Count(), Is.EqualTo(0));
			Assert.IsInstanceOf<CatalogViewModel>(shell.ActiveItem);
			Assert.AreEqual(shell.ActiveItem, catalog);
		}

		[Test]
		public void Activate_root_item()
		{
			shell.ShowPrice();
			var price = (PriceViewModel)shell.ActiveItem;
			price.CurrentPrice.Value = price.Prices.Value.FirstOrDefault();
			price.EnterPrice();
			Assert.That(shell.ActiveItem, Is.InstanceOf<PriceOfferViewModel>());
			shell.ShowCatalog();
			Assert.That(shell.ActiveItem, Is.InstanceOf<CatalogViewModel>());
		}

		[Test]
		public void Dispose_screen()
		{
			var screen = new BaseScreen();
			Activate(screen);
			screen.Dispose();
		}
	}
}
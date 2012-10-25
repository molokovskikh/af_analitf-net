using System.Linq;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class MnnFixture : BaseFixture
	{
		[Test]
		public void On_open_catalog_update_have_offers_flag()
		{
			var model = Init(new MnnViewModel());
			model.ShowWithoutOffers = true;
			model.CurrentMnn = model.Mnns.First(m => !m.HaveOffers);
			model.EnterMnn();

			var catalog = (CatalogViewModel)shell.ActiveItem;
			Assert.That(catalog.FilterByMnn, Is.True);
			Assert.That(catalog.FiltredMnn, Is.EqualTo(model.CurrentMnn));
			Assert.That(catalog.CatalogNames.Count, Is.GreaterThan(0));
			Assert.That(catalog.CatalogNames.All(n => n.Mnn.Id == model.CurrentMnn.Id), Is.True, catalog.CatalogNames.Implode());
			Assert.That(catalog.ShowWithoutOffers, Is.True);
		}
	}
}
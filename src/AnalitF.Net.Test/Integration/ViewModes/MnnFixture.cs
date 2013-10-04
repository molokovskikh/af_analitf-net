using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class MnnFixture : ViewModelFixture
	{
		[Test]
		public void On_open_catalog_update_have_offers_flag()
		{
			var catalogName = session.Query<CatalogName>().First(n => n.HaveOffers == false && n.Mnn != null);
			var model = Init(new MnnViewModel());
			Assert.That(model.Mnns.Value.Count, Is.GreaterThan(0));
			model.ShowWithoutOffers.Value = true;
			model.CurrentMnn = model.Mnns.Value.First(m => m.Id == catalogName.Mnn.Id);
			model.EnterMnn();

			var catalog = (CatalogViewModel)shell.ActiveItem;
			var names = (CatalogNameViewModel)catalog.ActiveItem;
			Assert.IsTrue(catalog.FilterByMnn, model.CurrentMnn.ToString());
			Assert.That(catalog.FiltredMnn, Is.EqualTo(model.CurrentMnn), model.CurrentMnn.ToString());
			Assert.That(names.CatalogNames.Value.Count, Is.GreaterThan(0), model.CurrentMnn.ToString());
			Assert.IsTrue(names.CatalogNames.Value.All(n => n.Mnn.Id == model.CurrentMnn.Id),
				names.CatalogNames.Value.Implode());
			Assert.That(catalog.ShowWithoutOffers, Is.True);
		}
	}
}
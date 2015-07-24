using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class MnnFixture : ViewModelFixture<MnnViewModel>
	{
		[Test]
		public void On_open_catalog_update_have_offers_flag()
		{
			var catalogName = session.Query<CatalogName>()
				.First(n => !n.HaveOffers
					&& n.Mnn != null
					&& !n.Mnn.HaveOffers);
			var items = model.Mnns;
			scheduler.Start();
			Assert.That(items.Value.Count, Is.GreaterThan(0));
			model.ShowWithoutOffers.Value = true;
			scheduler.Start();
			model.CurrentMnn = items.Value.First(m => m.Id == catalogName.Mnn.Id);
			model.EnterMnn();
			scheduler.Start();
			var catalog = (CatalogViewModel)shell.ActiveItem;
			var names = (CatalogNameViewModel)catalog.ActiveItem;
			Assert.IsTrue(catalog.FilterByMnn, model.CurrentMnn.ToString());
			Assert.That(catalog.FiltredMnn, Is.EqualTo(model.CurrentMnn), model.CurrentMnn.ToString());
			Assert.That(names.CatalogNames.Value.Count, Is.GreaterThan(0), model.CurrentMnn.ToString());
			Assert.IsTrue(names.CatalogNames.Value.All(n => n.Mnn.Id == model.CurrentMnn.Id),
				names.CatalogNames.Value.Implode());
			Assert.That(catalog.ShowWithoutOffers, Is.True);
		}

		[Test]
		public void Search()
		{
			var items = model.Mnns;
			scheduler.Start();
			var count = items.Value.Count;
			Assert.That(count, Is.GreaterThan(0));
			model.SearchBehavior.SearchText.Value = items.Value[0].Name;
			scheduler.AdvanceByMs(5000);
			scheduler.Start();
			Assert.That(model.Mnns.Value.Count, Is.LessThan(count));
			Assert.That(model.Mnns.Value.Count, Is.GreaterThan(0));
		}
	}
}
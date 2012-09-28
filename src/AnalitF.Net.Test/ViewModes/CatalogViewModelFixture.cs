using System.Linq;
using AnalitF.Net.Client.ViewModels;
using NUnit.Framework;

namespace AnalitF.Net.Test.ViewModes
{
	public class CatalogViewModelFixture : BaseFixture
	{
		private CatalogViewModel model;

		[SetUp]
		public void Setup()
		{
			model = new CatalogViewModel();
		}

		[Test]
		public void Show_catalog_view()
		{
			model.CurrentCatalogName = model.CatalogNames.First();
			model.ShowDescription();
		}

		[Test]
		public void Filter_by_mnn()
		{
			model.CurrentCatalogName = model.CatalogNames.First(n => n.Mnn != null);
			model.FilterByMnn = true;
			Assert.That(model.CatalogNames, Is.EquivalentTo(new[] { model.CurrentCatalogName }));
			model.FilterByMnn = false;
			Assert.That(model.CatalogNames.Count, Is.GreaterThan(1));
		}
	}
}
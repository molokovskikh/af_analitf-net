using System;
using System.Linq;
using AnalitF.Net.Client.ViewModels;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	public class CatalogViewModelFixture : BaseFixture
	{
		private CatalogViewModel model;

		[SetUp]
		public void Setup()
		{
			model = new CatalogViewModel();
		}

		[Test, RequiresSTA]
		public void Show_catalog_view()
		{
			model.CurrentCatalogName = model.CatalogNames.First();
			model.ShowDescription();
		}

		[Test]
		public void Filter_by_mnn()
		{
			ApplyMnnFilter();

			Assert.That(model.CatalogNames, Is.EquivalentTo(new[] { model.CurrentCatalogName }));
			model.FilterByMnn = false;
			Assert.That(model.CatalogNames.Count, Is.GreaterThan(1));
		}

		[Test]
		public void Filter_description()
		{
			ApplyMnnFilter();
			Assert.That(model.FilterDescription, Is.EqualTo(String.Format("Фильтр по \"{0}\"", model.FiltredMnn.Name)));

			model.CurrentFilter = model.Filters[1];
			Assert.That(model.FilterDescription, Is.EqualTo("Фильтр по жизненно важным"));
		}

		[Test]
		public void Filter_reset_mnn_filter()
		{
			ApplyMnnFilter();

			model.CurrentFilter = model.Filters[1];
			Assert.That(model.FilterByMnn, Is.False);
			Assert.That(model.FiltredMnn, Is.Null);
		}

		private void ApplyMnnFilter()
		{
			model.CurrentCatalogName = model.CatalogNames.First(n => n.Mnn != null);
			model.FilterByMnn = true;
		}
	}
}
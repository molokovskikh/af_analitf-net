using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	public class CatalogFixture : BaseFixture
	{
		private CatalogViewModel model;

		[SetUp]
		public void Setup()
		{
			model = Init(new CatalogViewModel());
		}

		[Test, RequiresSTA, Ignore]
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

		[Test]
		public void Reset_filter_on_escape()
		{
			ApplyMnnFilter();

			model.NavigateBackward();
			Assert.That(model.FilterByMnn, Is.False);
		}

		[Test]
		public void Do_change_search_text_if_result_not_found()
		{
			var changes = new List<string>();
			model.PropertyChanged += (sender, args) => changes.Add(args.PropertyName);
			model.SearchText += "ё";
			Assert.That(model.SearchText, Is.Null);
			Assert.That(changes, Is.Empty);
		}

		[Test]
		public void Open_offers_if_catalog_only_one()
		{
			var catalogId = session.Query<Catalog>().Where(c => c.HaveOffers)
				.GroupBy(c => c.Name)
				.Where(g => g.Count() == 1)
				.Select(g => g.Key)
				.ToList();
			model.CurrentCatalogName = model.CatalogNames.First(f => f.Id == catalogId[0].Id);
			model.EnterCatalogName();
			Assert.That(shell.ActiveItem, Is.InstanceOf<CatalogOfferViewModel>());
		}

		private void ApplyMnnFilter()
		{
			model.CurrentCatalogName = model.CatalogNames.First(n => n.Mnn != null);
			model.FilterByMnn = true;
		}
	}
}
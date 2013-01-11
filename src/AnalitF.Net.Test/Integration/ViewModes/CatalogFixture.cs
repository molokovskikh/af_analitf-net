using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	public class CatalogFixture : BaseFixture
	{
		private CatalogNameViewModel nameViewModel;
		private CatalogViewModel catalogModel;

		[SetUp]
		public void Setup()
		{
			catalogModel = Init(new CatalogViewModel());
			nameViewModel = (CatalogNameViewModel)catalogModel.ActiveItem;
		}

		[Test, RequiresSTA, Ignore]
		public void Show_catalog_view()
		{
			nameViewModel.CurrentCatalogName = nameViewModel.CatalogNames.First();
			catalogModel.ShowDescription();
		}

		[Test]
		public void Current_item()
		{
			Assert.That(catalogModel.ActiveItem, Is.InstanceOf<CatalogNameViewModel>());
			Assert.That(catalogModel.CurrentItem, Is.InstanceOf<CatalogName>());
		}

		[Test]
		public void Filter_by_mnn()
		{
			ApplyMnnFilter();

			Assert.That(nameViewModel.CatalogNames, Is.EquivalentTo(new[] { nameViewModel.CurrentCatalogName }));
			catalogModel.FilterByMnn = false;
			Assert.That(nameViewModel.CatalogNames.Count, Is.GreaterThan(1));
		}

		[Test]
		public void Filter_description()
		{
			ApplyMnnFilter();
			Assert.That(catalogModel.FilterDescription, Is.EqualTo(String.Format("Фильтр по \"{0}\"", catalogModel.FiltredMnn.Name)));

			catalogModel.CurrentFilter = catalogModel.Filters[1];
			Assert.That(catalogModel.FilterDescription, Is.EqualTo("Фильтр по жизненно важным"));
		}

		[Test]
		public void Filter_reset_mnn_filter()
		{
			ApplyMnnFilter();

			catalogModel.CurrentFilter = catalogModel.Filters[1];
			Assert.That(catalogModel.FilterByMnn, Is.False);
			Assert.That(catalogModel.FiltredMnn, Is.Null);
		}

		[Test]
		public void Reset_filter_on_escape()
		{
			ApplyMnnFilter();

			catalogModel.NavigateBackward();
			Assert.That(catalogModel.FilterByMnn, Is.False);
		}

		[Test]
		public void Do_change_search_text_if_result_not_found()
		{
			var changes = TrackChanges(nameViewModel);
			catalogModel.SearchText += "ё";
			Assert.That(catalogModel.SearchText, Is.Null);
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
			nameViewModel.CurrentCatalogName = nameViewModel.CatalogNames.First(f => f.Id == catalogId[0].Id);
			nameViewModel.EnterCatalogName();
			Assert.That(shell.ActiveItem, Is.InstanceOf<CatalogOfferViewModel>());
		}

		[Test]
		public void Activete_search()
		{
			catalogModel.CatalogSearch = true;
			var searchModel = (CatalogSearchViewModel)catalogModel.ActiveItem;

			var catalog = session.Query<Catalog>().First(c => c.HaveOffers);

			Assert.That(catalogModel.ActiveItem, Is.InstanceOf<CatalogSearchViewModel>());
			Assert.That(catalogModel.CurrentItem, Is.InstanceOf<Catalog>());
			var term = catalog.Name.Name.Slice(3);
			catalogModel.SearchText = term;
			searchModel.Search();
			Assert.That(searchModel.SearchText, Is.Empty);
			Assert.That(searchModel.ActiveSearchTerm, Is.EqualTo(term));
			Assert.That(searchModel.Catalogs.Count, Is.GreaterThan(0));
			Assert.That(searchModel.CurrentCatalog, Is.Not.Null);

			Assert.That(catalogModel.CurrentItem, Is.EqualTo(searchModel.CurrentCatalog));
			Assert.That(catalogModel.CurrentCatalog, Is.EqualTo(searchModel.CurrentCatalog));
			Assert.That(catalogModel.CurrentCatalogName, Is.EqualTo(searchModel.CurrentCatalog.Name));
		}

		[Test]
		public void Clear_search_term()
		{
			catalogModel.CatalogSearch = true;
			var searchModel = (CatalogSearchViewModel)catalogModel.ActiveItem;

			var catalog = session.Query<Catalog>().First(c => c.HaveOffers);
			var total = searchModel.Catalogs.Count;
			var term = catalog.Name.Name.Slice(3);
			catalogModel.SearchText = term;
			searchModel.Search();
			searchModel.ClearSearch();
			Assert.That(searchModel.ActiveSearchTerm, Is.Empty);
			Assert.That(searchModel.Catalogs.Count, Is.EqualTo(total));
		}

		[Test]
		public void Filter_by_mnn_in_search()
		{
			catalogModel.CatalogSearch = true;
			var searchModel = (CatalogSearchViewModel)catalogModel.ActiveItem;

			var catalog = session.Query<Catalog>().First(c => c.HaveOffers && c.Name.Mnn != null);
			var total = searchModel.Catalogs.Count;
			searchModel.CurrentCatalog = catalog;
			catalogModel.FilterByMnn = true;
			Assert.That(searchModel.Catalogs.Count, Is.LessThan(total));
		}

		[Test]
		public void Activate_item()
		{
			nameViewModel.CurrentCatalog = nameViewModel.Catalogs.First();
			nameViewModel.ActivateCatalog();
			Assert.That(catalogModel.CurrentItem, Is.InstanceOf<Catalog>());

			nameViewModel.ActivateCatalogName();
			Assert.That(catalogModel.CurrentItem, Is.InstanceOf<CatalogName>());
		}

		[Test]
		public void Search_in_catalog_names()
		{
			var name = (CatalogNameViewModel)catalogModel.ActiveItem;
			shell.ActiveItem = catalogModel;
			name.CurrentCatalog = name.Catalogs.First();
			name.EnterCatalog();
			var offer = (CatalogOfferViewModel)shell.ActiveItem;
			offer.SearchInCatalog("а");
			Assert.That(shell.ActiveItem, Is.InstanceOf<CatalogViewModel>());
			Assert.That(catalogModel.SearchText, Is.EqualTo("а"));
			Assert.That(catalogModel.CurrentCatalogName.Name.ToLower(), Is.StringStarting("а"));
		}

		private void ApplyMnnFilter()
		{
			nameViewModel.CurrentCatalogName = nameViewModel.CatalogNames.First(n => n.Mnn != null);
			catalogModel.FilterByMnn = true;
		}
	}
}
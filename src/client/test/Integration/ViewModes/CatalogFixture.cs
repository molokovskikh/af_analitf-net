using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Offers;
using Common.Tools;
using NHibernate.Linq;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using ReactiveUI.Testing;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	public class CatalogFixture : ViewModelFixture
	{
		private CatalogNameViewModel nameViewModel;
		private CatalogViewModel catalogModel;

		[SetUp]
		public void Setup()
		{
			catalogModel = Init(new CatalogViewModel());
			nameViewModel = (CatalogNameViewModel)catalogModel.ActiveItem;
			testScheduler.Start();
		}

		[Test, Ignore]
		public void Show_catalog_view()
		{
			nameViewModel.CurrentCatalogName.Value = nameViewModel.CatalogNames.Value.First();
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

			var expectedNames = session.Query<CatalogName>()
				.Where(n => n.Mnn.Id == nameViewModel.CurrentCatalogName.Value.Mnn.Id && n.HaveOffers)
				.ToArray();
			Assert.That(nameViewModel.CatalogNames.Value.Select(c => c.Name).ToList(),
				Is.EquivalentTo(expectedNames.Select(n => n.Name).ToArray()));
			catalogModel.FilterByMnn = false;
			testScheduler.Start();
			Assert.That(nameViewModel.CatalogNames.Value.Count, Is.GreaterThan(1));
		}

		[Test]
		public void Filter_description()
		{
			ApplyMnnFilter();
			Assert.That(catalogModel.FilterDescription,
				Is.EqualTo(String.Format("Фильтр по \"{0}\"", catalogModel.FiltredMnn.Name)));

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
			nameViewModel.CurrentCatalogName.Value = nameViewModel.CatalogNames.Value.First(f => f.Id == catalogId[0].Id);
			nameViewModel.EnterCatalogName();
			Assert.That(shell.ActiveItem, Is.InstanceOf<CatalogOfferViewModel>());
		}

		[Test]
		public void Activete_search()
		{
			catalogModel.CatalogSearch.Value = true;
			testScheduler.Start();
			var searchModel = (CatalogSearchViewModel)catalogModel.ActiveItem;

			var catalog = session.Query<Catalog>().First(c => c.HaveOffers);
			Assert.That(catalogModel.ActiveItem, Is.InstanceOf<CatalogSearchViewModel>());
			Assert.That(catalogModel.CurrentItem, Is.InstanceOf<Catalog>());
			var term = catalog.Name.Name.Slice(3);
			catalogModel.SearchText = term;
			searchModel.SearchBehavior.Search();
			Assert.That(searchModel.SearchBehavior.SearchText.Value, Is.Empty);
			Assert.That(searchModel.SearchBehavior.ActiveSearchTerm.Value, Is.EqualTo(term));
			Assert.That(searchModel.Items.Value.Count, Is.GreaterThan(0));
			Assert.That(searchModel.CurrentCatalog.Value, Is.Not.Null);

			Assert.That(catalogModel.CurrentItem, Is.EqualTo(searchModel.CurrentCatalog.Value));
			Assert.That(catalogModel.CurrentCatalog, Is.EqualTo(searchModel.CurrentCatalog.Value));
			Assert.That(catalogModel.CurrentCatalogName, Is.EqualTo(searchModel.CurrentCatalog.Value.Name));
		}

		[Test]
		public void Clear_search_term()
		{
			catalogModel.CatalogSearch.Value = true;
			testScheduler.Start();
			var searchModel = (CatalogSearchViewModel)catalogModel.ActiveItem;

			var catalog = session.Query<Catalog>().First(c => c.HaveOffers);
			var total = searchModel.Items.Value.Count;
			var term = catalog.Name.Name.Slice(3);
			catalogModel.SearchText = term;
			searchModel.SearchBehavior.Search();
			searchModel.SearchBehavior.ClearSearch();
			Assert.That(searchModel.SearchBehavior.ActiveSearchTerm.Value, Is.Empty);
			Assert.That(searchModel.Items.Value.Count, Is.EqualTo(total));
		}

		[Test]
		public void Filter_by_mnn_in_search()
		{
			catalogModel.CatalogSearch.Value = true;
			testScheduler.Start();
			var searchModel = (CatalogSearchViewModel)catalogModel.ActiveItem;

			var catalog = session.Query<Catalog>().First(c => c.HaveOffers && c.Name.Mnn != null);
			var total = searchModel.Items.Value.Count;
			searchModel.CurrentCatalog.Value = catalog;
			catalogModel.FilterByMnn = true;
			testScheduler.Start();
			Assert.That(searchModel.Items.Value.Count, Is.LessThan(total));
		}

		[Test]
		public void Activate_item()
		{
			nameViewModel.CurrentCatalog = nameViewModel.Catalogs.Value.First();
			nameViewModel.ActivateCatalog();
			Assert.That(catalogModel.CurrentItem, Is.InstanceOf<Catalog>());

			nameViewModel.ActivateCatalogName();
			Assert.That(catalogModel.CurrentItem, Is.InstanceOf<CatalogName>());
		}

		[Test]
		public void Search_in_catalog_names()
		{
			shell.ActiveItem = catalogModel;
			nameViewModel.CurrentCatalog = nameViewModel.Catalogs.Value.First();
			nameViewModel.EnterCatalog();

			var offer = (CatalogOfferViewModel)shell.ActiveItem;
			offer.SearchInCatalog(null, WpfTestHelper.TextArgs("а"));
			Assert.That(shell.ActiveItem, Is.InstanceOf<CatalogViewModel>());
			Assert.That(catalogModel.SearchText, Is.EqualTo("а"));
			Assert.That(catalogModel.CurrentCatalogName.Name.ToLower(), Is.StringStarting("а"));
		}

		[Test]
		public void Load_promotion()
		{
			var fixture = Fixture<LocalPromotion>();
			var catalog = fixture.Promotion.Catalogs.First();

			nameViewModel.CurrentCatalogName.Value = nameViewModel.CatalogNames.Value.First(n => n.Id == catalog.Name.Id);
			var name = nameViewModel.CurrentCatalogName;
			var promotions = nameViewModel.Promotions;
			Assert.IsFalse(promotions.Visible.Value);
			nameViewModel.ActivateCatalog();

			Assert.IsTrue(promotions.Visible.Value);
			Assert.AreEqual(name.Value.Name, promotions.Name.Value.Name);
			Assert.That(promotions.Promotions.Value.Count, Is.GreaterThan(0));
		}

		private void ApplyMnnFilter()
		{
			nameViewModel.CurrentCatalogName.Value = nameViewModel.CatalogNames.Value.First(n => n.Mnn != null);
			catalogModel.FilterByMnn = true;
			testScheduler.Start();
		}
	}
}
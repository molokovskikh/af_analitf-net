using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class CatalogSearchViewModel : BaseScreen
	{
		private List<Catalog> catalogs;
		private string searchText;
		private Catalog currentCatalog;
		private string _activeSearchTerm;

		public CatalogSearchViewModel(CatalogViewModel catalog)
		{
			ParentModel = catalog;
			QuickSearch = new QuickSearch<Catalog>(
				v => Catalogs.FirstOrDefault(c => c.Name.Name.ToLower().StartsWith(v)),
				v => CurrentCatalog = v);
			QuickSearch.IsEnabled = false;

			ParentModel.ObservableForProperty(m => (object)m.FilterByMnn)
				.Merge(ParentModel.ObservableForProperty(m => (object)m.CurrentFilter))
				.Merge(ParentModel.ObservableForProperty(m => (object)m.ShowWithoutOffers))
				.Subscribe(_ => Search());

			Search();
		}

		protected override ShellViewModel Shell
		{
			get
			{
				return (ShellViewModel)ParentModel.Parent;
			}
		}

		public CatalogViewModel ParentModel { get; set; }

		public QuickSearch<Catalog> QuickSearch { get; set; }

		public string SearchText
		{
			get { return searchText; }
			set
			{
				searchText = value;
				NotifyOfPropertyChange("SearchText");
			}
		}

		public string ActiveSearchTerm
		{
			get { return _activeSearchTerm; }
			set
			{
				_activeSearchTerm = value;
				NotifyOfPropertyChange("ActiveSearchTerm");
			}
		}

		public void Search()
		{
			if (!string.IsNullOrEmpty(SearchText) && SearchText.Length < 3)
				return;

			IQueryable<Catalog> query = StatelessSession.Query<Catalog>()
				.Fetch(c => c.Name)
				.ThenFetch(n => n.Mnn);
			if (!string.IsNullOrEmpty(searchText))
				query = query.Where(c => c.Name.Name.Contains(SearchText) || c.Form.Contains(SearchText));
			query = ParentModel.ApplyFilter(query);

			ActiveSearchTerm = SearchText;
			Catalogs = query
				.OrderBy(c => c.Name.Name)
				.ThenBy(c => c.Form)
				.ToList();

			if (CurrentCatalog == null)
				CurrentCatalog = Catalogs.FirstOrDefault();
		}

		public List<Catalog> Catalogs
		{
			get { return catalogs; }
			set
			{
				catalogs = value;
				NotifyOfPropertyChange("Catalogs");
			}
		}

		public Catalog CurrentCatalog
		{
			get { return currentCatalog; }
			set
			{
				currentCatalog = value;
				NotifyOfPropertyChange("CurrentCatalog");
			}
		}

		public void EnterCatalog()
		{
			if (CurrentCatalog == null || !CurrentCatalog.HaveOffers)
				return;

			Shell.Navigate(new CatalogOfferViewModel(CurrentCatalog));
		}
	}
}
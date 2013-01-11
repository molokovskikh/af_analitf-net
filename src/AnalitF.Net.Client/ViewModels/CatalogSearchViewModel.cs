using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using AnalitF.Net.Client.Helpers;
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
				.Subscribe(_ => Update());

			Update();
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

		public IResult Search()
		{
			if (string.IsNullOrEmpty(SearchText) || SearchText.Length < 3)
				return new HandledResult(false);

			ActiveSearchTerm = SearchText;
			SearchText = "";
			Update();
			return new HandledResult();
		}

		public void Update()
		{
			IQueryable<Catalog> query = StatelessSession.Query<Catalog>()
				.Fetch(c => c.Name)
				.ThenFetch(n => n.Mnn);
			if (!string.IsNullOrEmpty(ActiveSearchTerm))
				query = query.Where(c => c.Name.Name.Contains(ActiveSearchTerm) || c.Form.Contains(ActiveSearchTerm));
			query = ParentModel.ApplyFilter(query);

			if (ParentModel.FiltredMnn != null) {
				query = query.Where(c => c.Name.Mnn == ParentModel.FiltredMnn);
			}

			Catalogs = query.OrderBy(c => c.Name.Name).ThenBy(c => c.Form).ToList();

			if (CurrentCatalog == null)
				CurrentCatalog = Catalogs.FirstOrDefault();
		}

		public IResult ClearSearch()
		{
			if (!String.IsNullOrEmpty(SearchText)) {
				SearchText = "";
				return HandledResult.Handled();
			}

			if (String.IsNullOrEmpty(ActiveSearchTerm))
				return HandledResult.Skip();

			ActiveSearchTerm = "";
			Update();
			return HandledResult.Handled();
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
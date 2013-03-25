using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class CatalogSearchViewModel : BaseScreen
	{
		private List<Catalog> catalogs = new List<Catalog>();
		private string searchText;
		private Catalog currentCatalog;
		private string _activeSearchTerm;
		private CompositeDisposable disposable = new CompositeDisposable();

		public CatalogSearchViewModel(CatalogViewModel catalog)
		{
			ParentModel = catalog;
			QuickSearch = new QuickSearch<Catalog>(UiScheduler,
				v => Catalogs.FirstOrDefault(c => c.Name.Name.ToLower().StartsWith(v)),
				v => CurrentCatalog = v);
			QuickSearch.IsEnabled = false;

			//после закрытия формы нужно отписаться от событий родительской формы
			//что бы не делать лишних обновлений
			disposable.Add(ParentModel.ObservableForProperty(m => (object)m.FilterByMnn)
				.Merge(ParentModel.ObservableForProperty(m => (object)m.CurrentFilter))
				.Merge(ParentModel.ObservableForProperty(m => (object)m.ShowWithoutOffers))
				.Subscribe(_ => Update()));

			disposable.Add(this.ObservableForProperty(m => m.SearchText)
				.Throttle(Consts.SearchTimeout, Scheduler)
				.ObserveOn(UiScheduler)
				.Subscribe(_ => Search()));
		}

		protected override void OnDeactivate(bool close)
		{
			if (close)
				disposable.Dispose();

			base.OnDeactivate(close);
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

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
			if (string.IsNullOrEmpty(SearchText) || SearchText.Length < 3) {
				return new HandledResult(false);
			}

			ActiveSearchTerm = SearchText;
			SearchText = "";
			Update();
			return new HandledResult();
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

		public IResult EnterCatalog()
		{
			if (CurrentCatalog == null)
				return null;

			if (!CurrentCatalog.HaveOffers)
				return new ShowPopupResult();

			Shell.Navigate(new CatalogOfferViewModel(CurrentCatalog));
			return null;
		}
	}
}
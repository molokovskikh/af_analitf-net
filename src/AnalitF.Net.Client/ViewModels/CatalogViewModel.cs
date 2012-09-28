using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class CatalogViewModel : BaseScreen
	{
		private CatalogName currentCatalogName;
		private Catalog currentCatalogForm;
		private Catalog currentCatalog;

		private List<CatalogName> _catalogNames;
		private List<Catalog> _catalogForms;
		private bool showWithoutOffers;
		private string currentFilter;
		private Mnn filtredMnn;

		public CatalogViewModel()
		{
			DisplayName = "Поиск препаратов в каталоге";
			Filters = new [] {
				"Все",
				"Жизненноважные",
				"Обязательный ассортимент"
			};
			CurrentFilter = Filters[0];
			Update();

			this.ObservableForProperty(m => (object)m.FilterByMnn)
				.Merge(this.ObservableForProperty(m => (object)m.CurrentFilter))
				.Merge(this.ObservableForProperty(m => (object)m.ShowWithoutOffers))
				.Subscribe(_ => Update());

			this.ObservableForProperty(m => (object)m.CurrentFilter)
				.Subscribe(_ => LoadCatalogForms());
		}

		public List<CatalogName> CatalogNames
		{
			get { return _catalogNames; }
			set
			{
				_catalogNames = value;
				NotifyOfPropertyChange("CatalogNames");
			}
		}

		public List<Catalog> CatalogForms
		{
			get { return _catalogForms; }
			set
			{
				_catalogForms = value;
				NotifyOfPropertyChange("CatalogForms");
			}
		}

		public CatalogName CurrentCatalogName
		{
			get { return currentCatalogName; }
			set
			{
				currentCatalogName = value;
				RaisePropertyChangedEventImmediately("CurrentCatalogName");
				RaisePropertyChangedEventImmediately("CanShowDescription");
				LoadCatalogForms();
			}
		}

		public Catalog CurrentCatalogForm
		{
			get { return currentCatalogForm; }
			set
			{
				currentCatalogForm = value;
				CurrentCatalog = value;
				RaisePropertyChangedEventImmediately("CurrentCatalogForm");
			}
		}

		public Catalog CurrentCatalog
		{
			get { return currentCatalog; }
			set
			{
				currentCatalog = value;
				//если нас вызвала другая форма
				if (CurrentCatalogName == null) {
					CurrentCatalogName = currentCatalog.Name;
				}
				RaisePropertyChangedEventImmediately("CurrentCatalog");
			}
		}

		public bool ShowWithoutOffers
		{
			get { return showWithoutOffers; }
			set
			{
				showWithoutOffers = value;
				RaisePropertyChangedEventImmediately("ShowWithoutOffers");
			}
		}

		public string[] Filters { get; set; }

		public string CurrentFilter
		{
			get { return currentFilter; }
			set
			{
				currentFilter = value;
				RaisePropertyChangedEventImmediately("CurrentFilter");
			}
		}

		public bool FilterByMnn
		{
			get { return filtredMnn != null; }
			set
			{
				if (value) {
					if (CurrentCatalogName != null) {
						filtredMnn = CurrentCatalogName.Mnn;
					}
				}
				else {
					filtredMnn = null;
				}
				RaisePropertyChangedEventImmediately("FilterByMnn");
				RaisePropertyChangedEventImmediately("FiltredMnn");
			}
		}

		public Mnn FiltredMnn
		{
			get { return filtredMnn; }
			set
			{
				filtredMnn = value;
				RaisePropertyChangedEventImmediately("FiltredMnn");
				RaisePropertyChangedEventImmediately("FilterByMnn");
			}
		}


		private void Update()
		{
			var queryable = Session.Query<CatalogName>();
			if (!ShowWithoutOffers) {
				queryable = queryable.Where(c => c.HaveOffers);
			}
			if (CurrentFilter == Filters[1]) {
				queryable = queryable.Where(c => c.VitallyImportant);
			}
			if (CurrentFilter == Filters[2]) {
				queryable = queryable.Where(c => c.MandatoryList);
			}
			if (filtredMnn != null) {
				queryable = queryable.Where(n => n.Mnn == filtredMnn);
			}
			CatalogNames = queryable.OrderBy(c => c.Name).ToList();
		}

		private void LoadCatalogForms()
		{
			if (CurrentCatalogName == null)
				CatalogForms = Enumerable.Empty<Catalog>().ToList();

			var queryable = Session.Query<Catalog>().Where(c => c.Name == CurrentCatalogName);
			if (!ShowWithoutOffers) {
				queryable = queryable.Where(c => c.HaveOffers);
			}

			if (CurrentFilter == Filters[1]) {
				queryable = queryable.Where(c => c.VitallyImportant);
			}

			if (CurrentFilter == Filters[2]) {
				queryable = queryable.Where(c => c.MandatoryList);
			}

			CatalogForms = queryable
				.OrderBy(c => c.Form)
				.ToList();
		}

		public void EnterCatalogForms()
		{
			Shell.Navigate(new OfferViewModel(CurrentCatalog));
		}

		public bool CanShowDescription
		{
			get { return CurrentCatalogName != null && CurrentCatalogName.Description != null; }
		}

		public void ShowDescription()
		{
			if (!CanShowDescription)
				return;

			Manager.ShowDialog(new DescriptionViewModel(CurrentCatalogName.Description));
		}
	}
}
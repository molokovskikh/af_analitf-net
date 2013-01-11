using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Threading;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class FilterDeclaration
	{
		public FilterDeclaration(string name)
		{
			Name = name;
		}

		public FilterDeclaration(string name, string filterDescription, string filterDescriptionWithMnn)
		{
			Name = name;
			FilterDescription = filterDescription;
			FilterDescriptionWithMnn = filterDescriptionWithMnn;
		}

		public string Name { get; set; }
		public string FilterDescription { get; set; }
		public string FilterDescriptionWithMnn { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}

	[DataContract]
	public class CatalogViewModel : BaseScreen
	{
		private bool showWithoutOffers;
		private FilterDeclaration currentFilter;
		private Mnn filtredMnn;
		private bool viewOffersByCatalog;
		private Screen activeItem;
		private IDisposable observable = Disposable.Empty;

		public CatalogViewModel()
		{
			ViewOffersByCatalog = true;
			ViewOffersByCatalogEnabled = Settings.CanViewOffersByCatalogName;

			DisplayName = "Поиск препаратов в каталоге";
			Filters = new [] {
				new FilterDeclaration("Все"),
				new FilterDeclaration("Жизненно важные", "жизненно важным", "только жизненно важные"),
				new FilterDeclaration("Обязательный ассортимент", "обязательному ассортименту", "только обязательные ассортимент"),
			};
			CurrentFilter = Filters[0];

			CatalogSearch = false;

			this.ObservableForProperty(m => m.CurrentCatalogName)
				.Subscribe(_ => NotifyOfPropertyChange("CanShowDescription"));

			this.ObservableForProperty(m => m.CatalogSearch)
				.Subscribe(_ => NotifyOfPropertyChange("ViewOffersByCatalogVisible"));
		}

		public string SearchText
		{
			get
			{
				if (ActiveItem is CatalogNameViewModel)
					return ((CatalogNameViewModel)ActiveItem).CatalogNamesSearch.SearchText;
				else
					return ((CatalogSearchViewModel)ActiveItem).SearchText;
			}
			set
			{
				if (ActiveItem is CatalogNameViewModel)
					((CatalogNameViewModel)ActiveItem).CatalogNamesSearch.SearchText = value;
				else
					((CatalogSearchViewModel)ActiveItem).SearchText = value;
			}
		}

		public Screen ActiveItem
		{
			get { return activeItem; }
			set
			{
				activeItem = value;
				NotifyOfPropertyChange("ActiveItem");
			}
		}

		[DataMember]
		public bool ShowWithoutOffers
		{
			get { return showWithoutOffers; }
			set
			{
				showWithoutOffers = value;
				NotifyOfPropertyChange("ShowWithoutOffers");
			}
		}

		public FilterDeclaration[] Filters { get; set; }

		public FilterDeclaration CurrentFilter
		{
			get { return currentFilter; }
			set
			{
				currentFilter = value;
				FilterByMnn = false;
				NotifyOfPropertyChange("CurrentFilter");
				NotifyOfPropertyChange("FilterDescription");
			}
		}

		public CatalogName CurrentCatalogName
		{
			get
			{
				if (activeItem is CatalogSearchViewModel) {
					var catalog = ((CatalogSearchViewModel)activeItem).CurrentCatalog;
					return catalog == null ? null : catalog.Name;
				}
				return ((CatalogNameViewModel)activeItem).CurrentCatalogName;
			}
		}

		public Catalog CurrentCatalog
		{
			get
			{
				if (activeItem is CatalogSearchViewModel) {
					return ((CatalogSearchViewModel)activeItem).CurrentCatalog;
				}
				return ((CatalogNameViewModel)activeItem).CurrentCatalog;
			}
			set
			{
				if (activeItem is CatalogSearchViewModel) {
					((CatalogSearchViewModel)activeItem).CurrentCatalog = value;
				}
				((CatalogNameViewModel)activeItem).CurrentCatalog = value;
			}
		}

		public object CurrentItem
		{
			get
			{
				if (activeItem is CatalogSearchViewModel) {
					return ((CatalogSearchViewModel)activeItem).CurrentCatalog;
				}
				return ((CatalogNameViewModel)activeItem).CurrentItem;
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
				NotifyOfPropertyChange("FilterByMnn");
				NotifyOfPropertyChange("FiltredMnn");
				NotifyOfPropertyChange("FilterDescription");
			}
		}

		public Mnn FiltredMnn
		{
			get { return filtredMnn; }
			set
			{
				filtredMnn = value;
				if (!filtredMnn.HaveOffers)
					ShowWithoutOffers = true;
				NotifyOfPropertyChange("FiltredMnn");
				NotifyOfPropertyChange("FilterByMnn");
				NotifyOfPropertyChange("FilterDescription");
			}
		}

		public string FilterDescription
		{
			get
			{
				var parts = new List<string>();
				if (FiltredMnn != null)
					parts.Add(String.Format("\"{0}\"", FiltredMnn.Name));

				if (CurrentFilter != null) {
					var filter = FiltredMnn == null ? currentFilter.FilterDescription : currentFilter.FilterDescriptionWithMnn;
					if (!String.IsNullOrEmpty(filter))
						parts.Add(filter);
				}

				if (parts.Count > 0)
					parts.Insert(0, "Фильтр по");

				return parts.Implode(" ");
			}
		}

		public bool CanShowDescription
		{
			get
			{
				return CurrentCatalogName != null && CurrentCatalogName.Description != null;
			}
		}

		public void ShowDescription()
		{
			if (!CanShowDescription)
				return;

			var description = StatelessSession.Get<ProductDescription>(CurrentCatalogName.Description.Id);
			Manager.ShowDialog(new DescriptionViewModel(description));
		}

		public override void NavigateBackward()
		{
			if (FilterByMnn) {
				FilterByMnn = false;
				return;
			}

			if (!CurrentFilter.Name.Match("Все")) {
				CurrentFilter = Filters[0];
				return;
			}

			base.NavigateBackward();
		}

		public void SwitchMnnFilter()
		{
			FilterByMnn = !FilterByMnn;
		}

		public void SwitchViewOffersByCatalog()
		{
			if (ViewOffersByCatalogEnabled)
				ViewOffersByCatalog = !ViewOffersByCatalog;
		}

		[DataMember]
		public bool ViewOffersByCatalog
		{
			get { return viewOffersByCatalog; }
			set
			{
				viewOffersByCatalog = value;
				NotifyOfPropertyChange("ViewOffersByCatalog");
			}
		}

		public bool ViewOffersByCatalogVisible
		{
			get { return !CatalogSearch; }
		}

		public bool ViewOffersByCatalogEnabled { get; private set; }

		[DataMember]
		public bool CatalogSearch
		{
			get { return ActiveItem is CatalogSearchViewModel; }
			set
			{
				if (value && !(ActiveItem is CatalogSearchViewModel)) {
					observable.Dispose();
					var model = new CatalogSearchViewModel(this);
					observable = model.ObservableForProperty(m => m.CurrentCatalog)
						.Subscribe(_ => {
							NotifyOfPropertyChange("CurrentItem");
							NotifyOfPropertyChange("CurrentCatalog");
							NotifyOfPropertyChange("CurrentCatalogName");
						});
					ActiveItem = model;
				}
				else if (!(ActiveItem is CatalogNameViewModel)) {
					observable.Dispose();
					var model = new CatalogNameViewModel(this);
					var composite = new CompositeDisposable {
						model
							.ObservableForProperty(m => m.CurrentItem)
							.Subscribe(_ => NotifyOfPropertyChange("CurrentItem")),
						model
							.ObservableForProperty(m => m.CurrentCatalog)
							.Subscribe(_ => NotifyOfPropertyChange("CurrentCatalog")),
						model
							.ObservableForProperty(m => m.CurrentCatalogName)
							.Subscribe(_ => NotifyOfPropertyChange("CurrentCatalogName"))
					};
					observable = composite;
					ActiveItem = model;
				}
				NotifyOfPropertyChange("CatalogSearch");
			}
		}

		public IQueryable<Catalog> ApplyFilter(IQueryable<Catalog> queryable)
		{
			if (!ShowWithoutOffers) {
				queryable = queryable.Where(c => c.HaveOffers);
			}

			if (CurrentFilter == Filters[1]) {
				queryable = queryable.Where(c => c.VitallyImportant);
			}

			if (CurrentFilter == Filters[2]) {
				queryable = queryable.Where(c => c.MandatoryList);
			}
			return queryable;
		}
	}
}
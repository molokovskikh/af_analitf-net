﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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

	public class CatalogViewModel : BaseScreen
	{
		private CatalogName currentCatalogName;
		private Catalog currentCatalogForm;
		private Catalog currentCatalog;

		private List<CatalogName> _catalogNames;
		private List<Catalog> _catalogForms;
		private bool showWithoutOffers;
		private FilterDeclaration currentFilter;
		private Mnn filtredMnn;
		private bool viewOffersByCatalog;

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
			Update();

			CatalogNamesSearch = new QuickSearch<CatalogName>(
				v => CatalogNames.FirstOrDefault(n => n.Name.ToLower().StartsWith(v)),
				c => CurrentCatalogName = c);

			CatalogsSearch = new QuickSearch<Catalog>(
				v => CatalogForms.FirstOrDefault(n => n.Form.ToLower().StartsWith(v)),
				c => CurrentCatalogForm = c);

			this.ObservableForProperty(m => (object)m.FilterByMnn)
				.Merge(this.ObservableForProperty(m => (object)m.CurrentFilter))
				.Merge(this.ObservableForProperty(m => (object)m.ShowWithoutOffers))
				.Subscribe(_ => Update());

			this.ObservableForProperty(m => (object)m.CurrentFilter)
				.Subscribe(_ => LoadCatalogForms());
		}

		public string SearchText
		{
			get { return CatalogNamesSearch.SearchText; }
			set { CatalogNamesSearch.SearchText = value; }
		}

		public QuickSearch<CatalogName> CatalogNamesSearch { get; private set; }
		public QuickSearch<Catalog> CatalogsSearch { get; private set;}

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
				NotifyOfPropertyChange("CurrentCatalogName");
				NotifyOfPropertyChange("CanShowDescription");
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
				NotifyOfPropertyChange("CurrentCatalogForm");
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
				NotifyOfPropertyChange("CurrentCatalog");
			}
		}

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

			if (CurrentCatalogName == null)
				CurrentCatalogName = CatalogNames.FirstOrDefault();
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

		public void EnterCatalogForm()
		{
			Shell.Navigate(new CatalogOfferViewModel(CurrentCatalog));
		}

		//todo: если поставить фокус в строку поиска и ввести запрос
		//для товара который не отображен на экране
		//то выделение переместится к этому товару но прокрутка не будет произведена
		public IResult EnterCatalogName()
		{
			if (CurrentCatalogName == null || CatalogForms.Count == 0)
				return null;

			if (ViewOffersByCatalog) {
				Shell.Navigate(new CatalogOfferViewModel(CurrentCatalogName));
				return null;
			}

			if (CatalogForms.Count == 1) {
				CurrentCatalogForm = CatalogForms.First();
				EnterCatalogForm();
				return null;
			}
			else {
				return new FocusResult("CatalogForms");
			}
		}

		public bool CanShowDescription
		{
			get { return CurrentCatalogName != null && CurrentCatalogName.Description != null; }
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

		public void ShowDescription()
		{
			if (!CanShowDescription)
				return;

			Manager.ShowDialog(new DescriptionViewModel(CurrentCatalogName.Description));
		}

		public override void NavigateBackward()
		{
			if (!String.IsNullOrEmpty(SearchText)) {
				SearchText = null;
			}

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

		public bool ViewOffersByCatalog
		{
			get { return viewOffersByCatalog; }
			set
			{
				viewOffersByCatalog = value;
				NotifyOfPropertyChange("ViewOffersByCatalog");
			}
		}

		public bool ViewOffersByCatalogEnabled { get; private set; }
	}
}
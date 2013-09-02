﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class CatalogNameViewModel : BaseScreen
	{
		private CatalogName currentCatalogName;
		private Catalog currentCatalog;

		private Type activeItemType = typeof(CatalogName);

		public CatalogNameViewModel(CatalogViewModel catalogViewModel)
		{
			Readonly = true;
			ParentModel = catalogViewModel;
			CatalogNames = new NotifyValue<List<CatalogName>>();
			Catalogs = new NotifyValue<List<Catalog>>();
			CurrentItem = new NotifyValue<object>();

			CatalogNamesSearch = new QuickSearch<CatalogName>(UiScheduler,
				v => CatalogNames.Value.FirstOrDefault(n => n.Name.StartsWith(v, StringComparison.CurrentCultureIgnoreCase)),
				c => CurrentCatalogName = c);

			CatalogsSearch = new QuickSearch<Catalog>(UiScheduler,
				v => Catalogs.Value.FirstOrDefault(n => n.Form.StartsWith(v, StringComparison.CurrentCultureIgnoreCase)),
				c => CurrentCatalog = c);

			OnCloseDisposable.Add(ParentModel.ObservableForProperty(m => (object)m.FilterByMnn)
				.Merge(ParentModel.ObservableForProperty(m => (object)m.CurrentFilter))
				.Merge(ParentModel.ObservableForProperty(m => (object)m.ShowWithoutOffers))
				.Subscribe(_ => Update()));

			OnCloseDisposable.Add(ParentModel.ObservableForProperty(m => m.CurrentFilter)
				.Subscribe(_ => LoadCatalogs()));

			OnCloseDisposable.Add(ParentModel.ObservableForProperty(m => m.ViewOffersByCatalog)
				.Subscribe(_ => NotifyOfPropertyChange("CatalogsEnabled")));
		}

		protected override ShellViewModel Shell
		{
			get
			{
				return (ShellViewModel)ParentModel.Parent;
			}
		}

		public CatalogViewModel ParentModel { get; private set; }

		public QuickSearch<CatalogName> CatalogNamesSearch { get; private set; }

		public QuickSearch<Catalog> CatalogsSearch { get; private set;}

		public NotifyValue<List<CatalogName>> CatalogNames { get; set; }

		public CatalogName CurrentCatalogName
		{
			get { return currentCatalogName; }
			set
			{
				if (currentCatalogName == value)
					return;

				currentCatalogName = value;
				if (activeItemType == typeof(CatalogName))
					CurrentItem.Value = value;
				NotifyOfPropertyChange("CurrentCatalogName");
				LoadCatalogs();
			}
		}

		public bool CatalogsEnabled
		{
			get { return ParentModel.ViewOffersByCatalog; }
		}

		public Catalog CurrentCatalog
		{
			get { return currentCatalog; }
			set
			{
				if (currentCatalog == value)
					return;

				currentCatalog = value;
				//если нас вызвала другая форма
				if (CurrentCatalogName == null
					|| (CurrentCatalog != null && CurrentCatalog.Name.Id != CurrentCatalogName.Id)) {
					CurrentCatalogName = currentCatalog.Name;
				}

				if (activeItemType == typeof(Catalog))
					CurrentItem.Value = value;

				NotifyOfPropertyChange("CurrentCatalog");
			}
		}

		public NotifyValue<List<Catalog>> Catalogs { get; set; }

		public NotifyValue<object> CurrentItem { get; set; }

		public override void Update()
		{
			var queryable = StatelessSession.Query<CatalogName>();
			if (!ParentModel.ShowWithoutOffers) {
				queryable = queryable.Where(c => c.HaveOffers);
			}
			if (ParentModel.CurrentFilter == ParentModel.Filters[1]) {
				queryable = queryable.Where(c => c.VitallyImportant);
			}
			if (ParentModel.CurrentFilter == ParentModel.Filters[2]) {
				queryable = queryable.Where(c => c.MandatoryList);
			}
			if (ParentModel.FiltredMnn != null) {
				var mnnId = ParentModel.FiltredMnn.Id;
				queryable = queryable.Where(n => n.Mnn.Id == mnnId);
			}
			CatalogNames.Value = queryable.OrderBy(c => c.Name)
				.Fetch(c => c.Mnn)
				.ToList();

			if (CurrentCatalogName == null)
				CurrentCatalogName = CatalogNames.Value.FirstOrDefault();
		}

		private void LoadCatalogs()
		{
			if (CurrentCatalogName == null) {
				Catalogs.Value = Enumerable.Empty<Catalog>().ToList();
				return;
			}

			var nameId = CurrentCatalogName.Id;
			var queryable = StatelessSession.Query<Catalog>()
				.Fetch(c => c.Name)
				.ThenFetch(c => c.Mnn)
				.Where(c => c.Name.Id == nameId);
			queryable = ParentModel.ApplyFilter(queryable);

			Catalogs.Value = queryable
				.OrderBy(c => c.Form)
				.ToList();
		}

		//todo: если поставить фокус в строку поиска и ввести запрос
		//для товара который не отображен на экране
		//то выделение переместится к этому товару но прокрутка не будет произведена
		public IResult EnterCatalogName()
		{
			if (CurrentCatalogName == null || Catalogs.Value.Count == 0)
				return null;

			if (!ParentModel.ViewOffersByCatalog) {

				if (!CurrentCatalogName.HaveOffers)
					return new ShowPopupResult();

				Shell.Navigate(new CatalogOfferViewModel(CurrentCatalogName));
				return null;
			}

			if (Catalogs.Value.Count == 1) {
				CurrentCatalog = Catalogs.Value.First();
				return EnterCatalog();
			}
			return new FocusResult("Catalogs");
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

		public void ActivateCatalog()
		{
			activeItemType = typeof(Catalog);
			CurrentItem.Value = CurrentCatalog;
		}

		public void ActivateCatalogName()
		{
			activeItemType = typeof(CatalogName);
			CurrentItem.Value = CurrentCatalogName;
		}
	}
}
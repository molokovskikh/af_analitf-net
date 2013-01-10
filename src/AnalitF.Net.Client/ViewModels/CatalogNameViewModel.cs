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
	public class CatalogNameViewModel : BaseScreen
	{
		private CatalogName currentCatalogName;
		private Catalog currentCatalog;

		private List<Catalog> catalogs;
		private List<CatalogName> catalogNames;
		private object currentItem;
		private Type activeItemType = typeof(CatalogName);

		public CatalogNameViewModel(CatalogViewModel catalogViewModel)
		{
			ParentModel = catalogViewModel;

			CatalogNamesSearch = new QuickSearch<CatalogName>(
				v => CatalogNames.FirstOrDefault(n => n.Name.ToLower().StartsWith(v)),
				c => CurrentCatalogName = c);

			CatalogsSearch = new QuickSearch<Catalog>(
				v => Catalogs.FirstOrDefault(n => n.Form.ToLower().StartsWith(v)),
				c => CurrentCatalog = c);
			Update();

			ParentModel.ObservableForProperty(m => (object)m.FilterByMnn)
				.Merge(ParentModel.ObservableForProperty(m => (object)m.CurrentFilter))
				.Merge(ParentModel.ObservableForProperty(m => (object)m.ShowWithoutOffers))
				.Subscribe(_ => Update());

			ParentModel.ObservableForProperty(m => m.CurrentFilter)
				.Subscribe(_ => LoadCatalogs());

			ParentModel.ObservableForProperty(m => m.ViewOffersByCatalog)
				.Subscribe(_ => NotifyOfPropertyChange("CatalogsEnabled"));
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

		public List<CatalogName> CatalogNames
		{
			get { return catalogNames; }
			set
			{
				catalogNames = value;
				NotifyOfPropertyChange("CatalogNames");
			}
		}

		public CatalogName CurrentCatalogName
		{
			get { return currentCatalogName; }
			set
			{
				if (currentCatalogName == value)
					return;

				currentCatalogName = value;
				if (activeItemType == typeof(CatalogName))
					CurrentItem = value;
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
				if (CurrentCatalogName == null || CurrentCatalog.Name.Id != CurrentCatalogName.Id) {
					CurrentCatalogName = currentCatalog.Name;
				}

				if (activeItemType == typeof(Catalog))
					CurrentItem = value;

				NotifyOfPropertyChange("CurrentCatalog");
			}
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

		public object CurrentItem
		{
			get
			{
				return currentItem;
			}
			set
			{
				currentItem = value;
				NotifyOfPropertyChange("CurrentItem");
			}
		}

		private void Update()
		{
			var queryable = Session.Query<CatalogName>();
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
				queryable = queryable.Where(n => n.Mnn == ParentModel.FiltredMnn);
			}
			CatalogNames = queryable.OrderBy(c => c.Name).ToList();

			if (CurrentCatalogName == null)
				CurrentCatalogName = CatalogNames.FirstOrDefault();
		}

		private void LoadCatalogs()
		{
			if (CurrentCatalogName == null)
				Catalogs = Enumerable.Empty<Catalog>().ToList();

			var queryable = Session.Query<Catalog>().Where(c => c.Name == CurrentCatalogName);
			queryable = ParentModel.ApplyFilter(queryable);

			Catalogs = queryable
				.OrderBy(c => c.Form)
				.ToList();
		}

		//todo: если поставить фокус в строку поиска и ввести запрос
		//для товара который не отображен на экране
		//то выделение переместится к этому товару но прокрутка не будет произведена
		public IResult EnterCatalogName()
		{
			if (CurrentCatalogName == null || Catalogs.Count == 0)
				return null;

			if (!ParentModel.ViewOffersByCatalog) {

				if (CurrentCatalogName == null || !CurrentCatalogName.HaveOffers)
					return null;

				Shell.Navigate(new CatalogOfferViewModel(CurrentCatalogName));
				return null;
			}

			if (Catalogs.Count == 1) {
				CurrentCatalog = Catalogs.First();
				EnterCatalog();
				return null;
			}
			else {
				return new FocusResult("Catalogs");
			}
		}

		public void EnterCatalog()
		{
			if (CurrentCatalog == null || !CurrentCatalog.HaveOffers)
				return;

			Shell.Navigate(new CatalogOfferViewModel(CurrentCatalog));
		}

		public void ActivateCatalog()
		{
			activeItemType = typeof(Catalog);
			CurrentItem = CurrentCatalog;
		}

		public void ActivateCatalogName()
		{
			activeItemType = typeof(CatalogName);
			CurrentItem = CurrentCatalogName;
		}
	}
}
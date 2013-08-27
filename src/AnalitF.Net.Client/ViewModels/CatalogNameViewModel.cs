using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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

		private List<Catalog> catalogs = new List<Catalog>();
		private List<CatalogName> catalogNames = new List<CatalogName>();
		private object currentItem;
		private Type activeItemType = typeof(CatalogName);

		public CatalogNameViewModel(CatalogViewModel catalogViewModel)
		{
			ParentModel = catalogViewModel;

			CatalogNamesSearch = new QuickSearch<CatalogName>(UiScheduler,
				v => CatalogNames.FirstOrDefault(n => n.Name.ToLower().StartsWith(v)),
				c => CurrentCatalogName = c);

			CatalogsSearch = new QuickSearch<Catalog>(UiScheduler,
				v => Catalogs.FirstOrDefault(n => n.Form.ToLower().StartsWith(v)),
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
				if (CurrentCatalogName == null
					|| (CurrentCatalog != null && CurrentCatalog.Name.Id != CurrentCatalogName.Id)) {
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

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Update();
		}

		private void Update()
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
			CatalogNames = queryable.OrderBy(c => c.Name)
				.Fetch(c => c.Mnn)
				.ToList();

			if (CurrentCatalogName == null)
				CurrentCatalogName = CatalogNames.FirstOrDefault();
		}

		private void LoadCatalogs()
		{
			if (CurrentCatalogName == null) {
				Catalogs = Enumerable.Empty<Catalog>().ToList();
				return;
			}

			var nameId = CurrentCatalogName.Id;
			var queryable = StatelessSession.Query<Catalog>()
				.Fetch(c => c.Name)
				.ThenFetch(c => c.Mnn)
				.Where(c => c.Name.Id == nameId);
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

				if (!CurrentCatalogName.HaveOffers)
					return new ShowPopupResult();

				Shell.Navigate(new CatalogOfferViewModel(CurrentCatalogName));
				return null;
			}

			if (Catalogs.Count == 1) {
				CurrentCatalog = Catalogs.First();
				return EnterCatalog();
			}
			else {
				return new FocusResult("Catalogs");
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
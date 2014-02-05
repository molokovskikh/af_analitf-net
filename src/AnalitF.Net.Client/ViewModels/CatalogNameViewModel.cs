using System;
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
		private Catalog currentCatalog;

		private Type activeItemType = typeof(CatalogName);

		public CatalogNameViewModel(CatalogViewModel catalog)
		{
			Readonly = true;
			Shell = catalog.Shell;
			ParentModel = catalog;
			CatalogNames = new NotifyValue<List<CatalogName>>();
			Catalogs = new NotifyValue<List<Catalog>>(new List<Catalog>());
			CurrentItem = new NotifyValue<object>();
			CurrentCatalogName = new NotifyValue<CatalogName>();

			CatalogNamesSearch = new QuickSearch<CatalogName>(UiScheduler,
				v => CatalogNames.Value.FirstOrDefault(n => n.Name.StartsWith(v, StringComparison.CurrentCultureIgnoreCase)),
				c => CurrentCatalogName.Value = c);

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

			CurrentCatalogName.Changed()
				.Subscribe(_ => {
					if (activeItemType == typeof(CatalogName))
						CurrentItem.Value = CurrentCatalogName.Value;
				});

			//в жизни нет смысла грузить формы выпуска при каждом изменении наименования
			//это приведет к визуальным тормозам при скролинге
			//но в тестах это приведет к геморою
			CurrentCatalogName.Changed()
#if !DEBUG
				.Throttle(Consts.ScrollLoadTimeout)
				.ObserveOn(UiScheduler)
#endif
				.Subscribe(_ => LoadCatalogs(), CloseCancellation.Token);
		}

		public CatalogViewModel ParentModel { get; private set; }

		public QuickSearch<CatalogName> CatalogNamesSearch { get; private set; }
		public QuickSearch<Catalog> CatalogsSearch { get; private set;}

		public NotifyValue<List<CatalogName>> CatalogNames { get; set; }
		public NotifyValue<CatalogName> CurrentCatalogName { get; set; }

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
				if (currentCatalog != null) {
					if (CurrentCatalogName.Value == null
						|| CurrentCatalog.Name.Id != CurrentCatalogName.Value.Id) {
						CurrentCatalogName.Value = currentCatalog.Name;
					}
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

			if (CurrentCatalogName.Value == null)
				CurrentCatalogName.Value = CatalogNames.Value.FirstOrDefault();
		}

		private void LoadCatalogs()
		{
			if (StatelessSession == null)
				return;

			if (CurrentCatalogName.Value == null) {
				Catalogs.Value = Enumerable.Empty<Catalog>().ToList();
				return;
			}

			var nameId = CurrentCatalogName.Value.Id;
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
			if (CurrentCatalogName.Value == null || Catalogs.Value.Count == 0)
				return null;

			if (!ParentModel.ViewOffersByCatalog) {

				if (!CurrentCatalogName.Value.HaveOffers)
					return new ShowPopupResult();

				Shell.Navigate(new CatalogOfferViewModel(CurrentCatalogName.Value));
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
			CurrentItem.Value = CurrentCatalogName.Value;
		}
	}
}
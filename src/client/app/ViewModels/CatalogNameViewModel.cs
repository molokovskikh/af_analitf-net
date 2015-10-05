using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.ViewModels.Orders;
using AnalitF.Net.Client.ViewModels.Parts;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;
using NHibernate;
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
			Shell = catalog.Shell;
			ParentModel = catalog;
			CatalogNames = new NotifyValue<List<CatalogName>>(new List<CatalogName>());
			Catalogs = new NotifyValue<List<Catalog>>(new List<Catalog>());
			CurrentItem = new NotifyValue<object>();
			CurrentCatalogName = new NotifyValue<CatalogName>();

			CatalogNamesSearch = new QuickSearch<CatalogName>(UiScheduler,
				v => CatalogNames.Value.FirstOrDefault(n => n.Name.StartsWith(v, StringComparison.CurrentCultureIgnoreCase)),
				CurrentCatalogName);
			CatalogNamesSearch.RemapChars = true;

			CatalogsSearch = new QuickSearch<Catalog>(UiScheduler,
				v => Catalogs.Value.FirstOrDefault(n => n.Form.StartsWith(v, StringComparison.CurrentCultureIgnoreCase)),
				c => CurrentCatalog = c);
			CatalogsSearch.RemapChars = true;

			OnCloseDisposable.Add(ParentModel.ObservableForProperty(m => m.CurrentFilter).Cast<object>()
				.Merge(ParentModel.ObservableForProperty(m => m.ShowWithoutOffers))
				.Subscribe(_ => LoadCatalogs()));

			OnCloseDisposable.Add(ParentModel.ObservableForProperty(m => m.ViewOffersByCatalog)
				.Subscribe(_ => NotifyOfPropertyChange("CatalogsEnabled")));

			CurrentCatalogName
				.Subscribe(_ => {
					if (activeItemType == typeof(CatalogName))
						CurrentItem.Value = CurrentCatalogName.Value;
				});

			//в жизни нет смысла грузить формы выпуска при каждом изменении наименования
			//это приведет к визуальным тормозам при скролинге
			//но в тестах это приведет к геморою
			CurrentCatalogName
#if !DEBUG
				.Throttle(Consts.ScrollLoadTimeout)
				.ObserveOn(UiScheduler)
#endif
				.Subscribe(_ => LoadCatalogs(), CloseCancellation.Token);
			ExcelExporter.ActiveProperty.Value = "CatalogNames";
		}

		public CatalogViewModel ParentModel { get; private set; }

		public PromotionPopup Promotions { get; set; }

		public QuickSearch<CatalogName> CatalogNamesSearch { get; private set; }
		public QuickSearch<Catalog> CatalogsSearch { get; private set;}

		[Export]
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

		[Export]
		public NotifyValue<List<Catalog>> Catalogs { get; set; }

		public NotifyValue<object> CurrentItem { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Promotions = new PromotionPopup(Shell.Config, CurrentCatalogName, RxQuery, Env);
			ParentModel.ObservableForProperty(m => (object)m.FilterByMnn, skipInitial: false)
				.Merge(ParentModel.ObservableForProperty(m => (object)m.CurrentFilter))
				.Merge(ParentModel.ObservableForProperty(m => (object)m.ShowWithoutOffers))
				.Select(_ => RxQuery(LoadCatalogNames))
				.Switch()
				.ObserveOn(UiScheduler)
				.Subscribe(CatalogNames, CloseCancellation.Token);
			CatalogNames.Subscribe(_ => {
				CurrentCatalogName.Value = CurrentCatalogName.Value
					?? (CatalogNames.Value ?? Enumerable.Empty<CatalogName>()).FirstOrDefault();
			});
		}

		protected override void OnActivate()
		{
			base.OnActivate();

			if (!String.IsNullOrEmpty(CatalogNamesSearch.SearchText)) {
				CatalogNamesSearch.searchInProgress = true;
				Dispatcher.CurrentDispatcher.BeginInvoke(
					DispatcherPriority.Loaded,
					new System.Action(() => {
						var view = Views.Values.OfType<CatalogNameView>().FirstOrDefault();
						var el = (DataGrid)view.FindName("CatalogNames");
						DataGridHelper.Focus(el);
						CatalogNamesSearch.searchInProgress = false;
					}));
			}
		}

		private List<CatalogName> LoadCatalogNames(IStatelessSession session)
		{
			var queryable = session.Query<CatalogName>();
			if (ParentModel.ShowWithoutOffers) {
				if (ParentModel.CurrentFilter == ParentModel.Filters[1])
					queryable = queryable.Where(c => c.VitallyImportant);

				if (ParentModel.CurrentFilter == ParentModel.Filters[2])
					queryable = queryable.Where(c => c.MandatoryList);

				if (ParentModel.CurrentFilter == ParentModel.Filters[3])
					queryable = queryable.Where(n => session.Query<AwaitedItem>().Any(i => i.Catalog.Name == n));
			}
			else {
				queryable = queryable.Where(c => c.HaveOffers);
				if (ParentModel.CurrentFilter == ParentModel.Filters[1])
					queryable = queryable.Where(n => session.Query<Catalog>().Any(c => c.Name == n && c.HaveOffers && c.VitallyImportant));

				if (ParentModel.CurrentFilter == ParentModel.Filters[2])
					queryable = queryable.Where(n => session.Query<Catalog>().Any(c => c.Name == n && c.HaveOffers && c.MandatoryList));

				if (ParentModel.CurrentFilter == ParentModel.Filters[3])
					queryable = queryable.Where(n => session.Query<Catalog>().Any(c => c.Name == n && c.HaveOffers && session.Query<AwaitedItem>().Any(i => i.Catalog == c)));
			}

			if (ParentModel.FiltredMnn != null) {
				var mnnId = ParentModel.FiltredMnn.Id;
				queryable = queryable.Where(n => n.Mnn.Id == mnnId);
			}
			return queryable.OrderBy(c => c.Name)
				.Fetch(c => c.Mnn)
				.ToList();
		}

		private void LoadCatalogs()
		{
			if (StatelessSession == null)
				return;

			if (CurrentCatalogName.Value == null) {
				Catalogs.Value = Enumerable.Empty<Catalog>().ToList();
				return;
			}

			//сессия может использоваться для асинхронной загрузки данных выполняем синхронизацию
			var nameId = CurrentCatalogName.Value.Id;
			var queryable = StatelessSession.Query<Catalog>()
				.Fetch(c => c.Name)
				.ThenFetch(c => c.Mnn)
				.Where(c => c.Name.Id == nameId);

			if (!ParentModel.ShowWithoutOffers)
				queryable = queryable.Where(c => c.HaveOffers);

			if (ParentModel.CurrentFilter == ParentModel.Filters[1])
				queryable = queryable.Where(c => c.VitallyImportant);

			if (ParentModel.CurrentFilter == ParentModel.Filters[2])
				queryable = queryable.Where(c => c.MandatoryList);

			if (ParentModel.CurrentFilter == ParentModel.Filters[3])
				queryable = queryable.Where(c => StatelessSession.Query<AwaitedItem>().Any(i => i.Catalog == c));

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
					return new ShowPopupResult(() => ParentModel.ShowOrderHistory());

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
				return new ShowPopupResult(() => ParentModel.ShowOrderHistory());

			Shell.Navigate(new CatalogOfferViewModel(CurrentCatalog));
			return null;
		}

		public void ActivateCatalog()
		{
			if (activeItemType != typeof(Catalog)) {
				ExcelExporter.ActiveProperty.Value = "Catalogs";
				activeItemType = typeof(Catalog);
				CurrentItem.Value = CurrentCatalog;
			}
		}

		public void ActivateCatalogName()
		{
			if (activeItemType != typeof(CatalogName)) {
				ExcelExporter.ActiveProperty.Value = "CatalogNames";
				activeItemType = typeof(CatalogName);
				CurrentItem.Value = CurrentCatalogName.Value;
				Promotions.Hide();
			}
		}
	}
}
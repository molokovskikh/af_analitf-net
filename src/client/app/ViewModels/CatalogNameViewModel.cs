﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.ViewModels.Orders;
using AnalitF.Net.Client.ViewModels.Parts;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;
using Common.Tools;
using Dapper;

namespace AnalitF.Net.Client.ViewModels
{
	public class CatalogNameViewModel : BaseScreen
	{
		private Catalog currentCatalog;

		private Type activeItemType = typeof(CatalogName);

		public CatalogNameViewModel(CatalogViewModel catalog)
		{
			InitFields();
			Shell = catalog.Shell;
			ParentModel = catalog;
			CatalogNames.Value = new List<CatalogName>();
			Catalogs.Value = new List<Catalog>();

			CatalogNamesSearch = new QuickSearch<CatalogName>(UiScheduler,
				v => CatalogNames.Value.FirstOrDefault(n => n.Name.StartsWith(v, StringComparison.CurrentCultureIgnoreCase)),
				CurrentCatalogName);

			CatalogsSearch = new QuickSearch<Catalog>(UiScheduler,
				v => Catalogs.Value.FirstOrDefault(n => n.Form.StartsWith(v, StringComparison.CurrentCultureIgnoreCase)),
				c => CurrentCatalog = c);

			ParentModel.ObservableForProperty(m => m.ViewOffersByCatalog, skipInitial: false)
				.Select(x => x.Value)
				.Subscribe(CatalogsEnabled, CloseCancellation.Token);

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
				.Merge(ParentModel.ObservableForProperty(m => m.CurrentFilter).Cast<object>())
				.Merge(ParentModel.ObservableForProperty(m => m.ShowWithoutOffers))
				.Select(_ => {
					if (CurrentCatalogName.Value == null) {
						return Observable.Return(Enumerable.Empty<Catalog>().ToList());
					}
					var nameId = CurrentCatalogName.Value.Id;
					return Env.RxQuery(s => {
						var queryable = s.Query<Catalog>()
							.Fetch(c => c.Name)
							.ThenFetch(c => c.Mnn)
							.Where(c => c.Name.Id == nameId);

						if (!ParentModel.ShowWithoutOffers) {
							if (ParentModel.Mode == CatalogViewMode.Basic)
								queryable = queryable.Where(c => c.HaveOffers);
							else if (ParentModel.Mode == CatalogViewMode.CatalogSelector) {
								var catalogIds = s.Query<WaybillLine>().Where(x => x.Waybill.Status == DocStatus.Posted).Select(x => x.CatalogId).Distinct().ToList();
								queryable = queryable.Where(c => catalogIds.Contains(c.Id));
							}
						}

						if (ParentModel.CurrentFilter == ParentModel.Filters[1])
							queryable = queryable.Where(c => c.VitallyImportant);

						if (ParentModel.CurrentFilter == ParentModel.Filters[2])
							queryable = queryable.Where(c => c.MandatoryList);

						if (ParentModel.CurrentFilter == ParentModel.Filters[3])
							queryable = queryable.Where(c => s.Query<AwaitedItem>().Any(i => i.Catalog == c));

						return queryable.OrderBy(c => c.Form).ToList();
					});
				})
				.Switch()
				.Subscribe(Catalogs, CloseCancellation.Token);
			ExcelExporter.ActiveProperty.Value = "CatalogNames";
		}

		public CatalogViewModel ParentModel { get; }

		public PromotionPopup Promotions { get; set; }
		public ProducerPromotionPopup ProducerPromotions { get; set; }

		public QuickSearch<CatalogName> CatalogNamesSearch { get; }
		public QuickSearch<Catalog> CatalogsSearch { get; }

		[Export]
		public NotifyValue<List<CatalogName>> CatalogNames { get; set; }
		public NotifyValue<CatalogName> CurrentCatalogName { get; set; }

		public NotifyValue<bool> CatalogsEnabled { get; set; }

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

				NotifyOfPropertyChange(nameof(CurrentCatalog));
			}
		}

		[Export]
		public NotifyValue<List<Catalog>> Catalogs { get; set; }

		public NotifyValue<object> CurrentItem { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Promotions = new PromotionPopup(Shell.Config, CurrentCatalogName, Env);
			ParentModel.ObservableForProperty(m => (object)m.FilterByMnn, skipInitial: false)
				.Merge(ParentModel.ObservableForProperty(m => (object)m.CurrentFilter))
				.Merge(ParentModel.ObservableForProperty(m => (object)m.ShowWithoutOffers))
				.Select(_ => RxQuery(LoadCatalogNames))
				.Switch()
				.Subscribe(CatalogNames, CloseCancellation.Token);
			CatalogNames.Subscribe(_ => {
				CurrentCatalogName.Value = CurrentCatalogName.Value
					?? (CatalogNames.Value ?? Enumerable.Empty<CatalogName>()).FirstOrDefault();
			});

			ProducerPromotions = new ProducerPromotionPopup(Shell.Config, CurrentCatalogName, Env);

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
				var filterType = (ParentModel.CurrentFilter?.FilterType).GetValueOrDefault();
			if (ParentModel.ShowWithoutOffers)
			{

				if (ParentModel.CurrentFilter == ParentModel.Filters[1])
					queryable = queryable.Where(c => c.VitallyImportant);

				if (ParentModel.CurrentFilter == ParentModel.Filters[2])
					queryable = queryable.Where(c => c.MandatoryList);

				if (ParentModel.CurrentFilter == ParentModel.Filters[3])
					queryable = queryable.Where(n => session.Query<AwaitedItem>().Any(i => i.Catalog.Name == n));
				if (filterType == FilterType.PKU)
					queryable = queryable.Where(c => c.Narcotic || c.Toxic || c.Combined || c.Other);
				if (filterType == FilterType.PKUNarcotic)
					queryable = queryable.Where(c => c.Narcotic);
				if (filterType == FilterType.PKUToxic)
					queryable = queryable.Where(c => c.Toxic);
				if (filterType == FilterType.PKUCombined)
					queryable = queryable.Where(c => c.Combined);
				if (filterType == FilterType.PKUOther)
					queryable = queryable.Where(c => c.Other);

				if ((ParentModel.CurrentFiltercategory != null) && (ParentModel.CurrentFiltercategory.Id > 0)){
					queryable = queryable.Where(n => session.Query<Catalog>()
											.Any((c) => (c.Name == n) && ((c.Category != null) && (c.Category.Id == ParentModel.CurrentFiltercategory.Id))));
				}
				
			} else {

				if (ParentModel.Mode == CatalogViewMode.Basic) {

					queryable = queryable.Where(c => c.HaveOffers);
					if (ParentModel.CurrentFilter == ParentModel.Filters[1])
						queryable = queryable.Where(n => session.Query<Catalog>().Any(c => c.Name == n && c.HaveOffers && c.VitallyImportant));

					if (ParentModel.CurrentFilter == ParentModel.Filters[2])
						queryable = queryable.Where(n => session.Query<Catalog>().Any(c => c.Name == n && c.HaveOffers && c.MandatoryList));

					if (ParentModel.CurrentFilter == ParentModel.Filters[3])
						queryable = queryable.Where(n => session.Query<Catalog>().Any(c => c.Name == n && c.HaveOffers
							&& session.Query<AwaitedItem>().Any(i => i.Catalog == c)));

					if (filterType == FilterType.PKU)
						queryable = queryable.Where(n => session.Query<Catalog>()
						.Any(c => c.Name == n && c.HaveOffers && (c.Narcotic || c.Toxic || c.Combined || c.Other)));
					if (filterType == FilterType.PKUNarcotic)
						queryable = queryable.Where(n => session.Query<Catalog>().Any(c => c.Name == n && c.HaveOffers && c.Narcotic));
					if (filterType == FilterType.PKUToxic)
						queryable = queryable.Where(n => session.Query<Catalog>().Any(c => c.Name == n && c.HaveOffers && c.Toxic));
					if (filterType == FilterType.PKUCombined)
						queryable = queryable.Where(n => session.Query<Catalog>().Any(c => c.Name == n && c.HaveOffers && c.Combined));
					if (filterType == FilterType.PKUOther)
						queryable = queryable.Where(n => session.Query<Catalog>().Any(c => c.Name == n && c.HaveOffers && c.Other));

					if ((ParentModel.CurrentFiltercategory != null) && (ParentModel.CurrentFiltercategory.Id > 0)){
						queryable = queryable.Where(n => session.Query<Catalog>()
								.Any((c) => (c.Name == n) && c.HaveOffers && ((c.Category != null) && (c.Category.Id == ParentModel.CurrentFiltercategory.Id))));
					}																																		
					
				}
				else if (ParentModel.Mode == CatalogViewMode.CatalogSelector)
				{
					var catalogIds = session.Query<WaybillLine>().Where(x => x.Waybill.Status == DocStatus.Posted).Select(x => x.CatalogId).Distinct().ToList();
					var catalogNameIds = session.Query<Catalog>().Where(x => catalogIds.Contains(x.Id)).Select(x => x.Name.Id).Distinct().ToList();
					queryable = queryable.Where(c => catalogNameIds.Contains(c.Id));
					if (ParentModel.CurrentFilter == ParentModel.Filters[1])
						queryable = queryable.Where(n => session.Query<Catalog>().Any(c => c.Name == n && catalogIds.Contains(c.Id) && c.VitallyImportant));

					if (ParentModel.CurrentFilter == ParentModel.Filters[2])
						queryable = queryable.Where(n => session.Query<Catalog>().Any(c => c.Name == n && catalogIds.Contains(c.Id) && c.MandatoryList));

					if (ParentModel.CurrentFilter == ParentModel.Filters[3])
						queryable = queryable.Where(n => session.Query<Catalog>().Any(c => c.Name == n && catalogIds.Contains(c.Id)
							&& session.Query<AwaitedItem>().Any(i => i.Catalog == c)));

					if (filterType == FilterType.PKU)
						queryable = queryable.Where(n => session.Query<Catalog>()
						.Any(c => c.Name == n && catalogIds.Contains(c.Id) && (c.Narcotic || c.Toxic || c.Combined || c.Other)));
					if (filterType == FilterType.PKUNarcotic)
						queryable = queryable.Where(n => session.Query<Catalog>().Any(c => c.Name == n && catalogIds.Contains(c.Id) && c.Narcotic));
					if (filterType == FilterType.PKUToxic)
						queryable = queryable.Where(n => session.Query<Catalog>().Any(c => c.Name == n && catalogIds.Contains(c.Id) && c.Toxic));
					if (filterType == FilterType.PKUCombined)
						queryable = queryable.Where(n => session.Query<Catalog>().Any(c => c.Name == n && catalogIds.Contains(c.Id) && c.Combined));
					if (filterType == FilterType.PKUOther)
						queryable = queryable.Where(n => session.Query<Catalog>().Any(c => c.Name == n && catalogIds.Contains(c.Id) && c.Other));

					if ((ParentModel.CurrentFiltercategory != null) && (ParentModel.CurrentFiltercategory.Id > 0)){
						queryable = queryable.Where(n => session.Query<Catalog>()
												.Any((c) => (c.Name == n) && catalogIds.Contains(c.Id) && ((c.Category!=null)&&(c.Category.Id == ParentModel.CurrentFiltercategory.Id))));
					}
	
				}
			}

			if (ParentModel.FiltredMnn != null) {
				var mnnId = ParentModel.FiltredMnn.Id;
				queryable = queryable.Where(n => n.Mnn.Id == mnnId);
			}
			return queryable.OrderBy(c => c.Name)
				.Fetch(c => c.Mnn)
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

			if (ParentModel.Mode == CatalogViewMode.Basic) {
				if (!CurrentCatalog.HaveOffers)
					return new ShowPopupResult(() => ParentModel.ShowOrderHistory());

				Shell.Navigate(new CatalogOfferViewModel(CurrentCatalog));
			}
			else if (ParentModel.Mode == CatalogViewMode.CatalogSelector)
				ParentModel.CatalogSelector(CurrentCatalog);

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
				ProducerPromotions.Hide();
			}
		}
	}

}
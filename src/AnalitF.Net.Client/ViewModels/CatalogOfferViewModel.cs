using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using Common.Tools;
using NHibernate.Linq;
using ReactiveUI;
using System.Reactive.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class CatalogOfferViewModel : BaseOfferViewModel, IPrintable
	{
		private string currentRegion;
		private List<string> regions;
		private string currentFilter;
		private bool groupByProduct;

		private decimal retailMarkup;
		private List<MaxProducerCost> maxProducerCosts;

		private CatalogName filterCatalogName;
		private Catalog filterCatalog;

		private CatalogOfferViewModel()
		{
			NeedToCalculateDiff = true;
			DisplayName = "Сводный прайс-лист";
			GroupByProduct = Settings.GroupByProduct;
			Filters = new [] { "Все", "Основные", "Неосновные" };
			CurrentFilter = Filters[0];
			CurrentRegion = Consts.AllRegionLabel;
			CurrentProducer = Consts.AllProducerLabel;

			this.ObservableForProperty(m => m.CurrentRegion)
				.Merge(this.ObservableForProperty(m => m.CurrentProducer))
				.	Merge(this.ObservableForProperty(m => m.CurrentFilter))
				.Subscribe(e => Update());

			this.ObservableForProperty(m => m.CurrentOffer)
				.Subscribe(_ => NotifyOfPropertyChange("RetailMarkup"));

			this.ObservableForProperty(m => m.RetailMarkup)
				.Subscribe(_ => NotifyOfPropertyChange("RetailCost"));
		}

		public CatalogOfferViewModel(Catalog catalog)
			: this()
		{
			filterCatalog = catalog;
			Name = catalog.FullName;
			//тк мы фильтруем по каталожному продукту то нет нужды загружать его
			CurrentCatalog = catalog;
		}

		public CatalogOfferViewModel(CatalogName catalogName)
			: this()
		{
			filterCatalogName = catalogName;
			Name = filterCatalogName.Name;
		}

		public string Name { get; private set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Update();
			UpdateMaxProducers();

			if (CurrentOffer == null)
				CurrentOffer = Offers.FirstOrDefault(o => o.Price.BasePrice);

			if (CurrentOffer == null)
				CurrentOffer = offers.FirstOrDefault();

			UpdateRegions();
			UpdateProducers();
		}

		protected override void OnActivate()
		{
			base.OnActivate();

			if (Offers.Count == 0) {
				Manager.Warning("Нет предложений");
				IsSuccessfulActivated = false;
			}
		}

		private void UpdateMaxProducers()
		{
			if (CurrentCatalog == null)
				return;

			var catalogId = CurrentCatalog.Id;
			MaxProducerCosts = StatelessSession.Query<MaxProducerCost>()
				.Where(c => c.CatalogId == catalogId)
				.OrderBy(c => c.Product)
				.ThenBy(c => c.Producer)
				.ToList();
		}

		private void UpdateRegions()
		{
			var offerRegions = Offers.Select(o => o.Price).Distinct().Select(p => p.RegionName).OrderBy(r => r).ToList();
			Regions = new[] { Consts.AllRegionLabel }.Concat(offerRegions).ToList();
		}

		protected override void Query()
		{
			Catalog[] catalogs = null;
			IQueryable<Offer> queryable;
			if (filterCatalog != null) {
				var catalogId = filterCatalog.Id;
				queryable = StatelessSession.Query<Offer>().Where(o => o.CatalogId == catalogId);
			}
			else {
				catalogs = StatelessSession.Query<Catalog>()
					.Fetch(c => c.Name)
					.Where(c => c.Name == filterCatalogName).ToArray();
				var ids = catalogs.Select(c => c.Id).ToArray();
				queryable = StatelessSession.Query<Offer>()
					.Where(o => ids.Contains(o.CatalogId));
			}

			if (CurrentRegion != Consts.AllRegionLabel) {
				queryable = queryable.Where(o => o.Price.RegionName == CurrentRegion);
			}
			if (CurrentProducer != Consts.AllProducerLabel) {
				queryable = queryable.Where(o => o.Producer == CurrentProducer);
			}
			if (CurrentFilter == Filters[1]) {
				queryable = queryable.Where(o => o.Price.BasePrice);
			}
			if (CurrentFilter == Filters[2]) {
				queryable = queryable.Where(o => !o.Price.BasePrice);
			}

			var offers = queryable.Fetch(o => o.Price).ToList();
			offers = Sort(offers);
			if (IsFilterByCatalogName) {
				offers.Each(o => o.GroupName = catalogs.Where(c => c.Id == o.CatalogId).Select(c => c.FullName).FirstOrDefault());
			}
			Offers = offers;
		}

		public bool IsFilterByCatalogName
		{
			get
			{
				return filterCatalogName != null;
			}
		}

		public string[] Filters { get; set; }

		public List<MaxProducerCost> MaxProducerCosts
		{
			get { return maxProducerCosts; }
			set
			{
				maxProducerCosts = value;
				NotifyOfPropertyChange("MaxProducerCosts");
			}
		}

		public string CurrentFilter
		{
			get { return currentFilter; }
			set
			{
				currentFilter = value;
				NotifyOfPropertyChange("CurrentFilter");
			}
		}

		public List<string> Regions
		{
			get { return regions; }
			set
			{
				regions = value;
				NotifyOfPropertyChange("Regions");
			}
		}

		public string CurrentRegion
		{
			get { return currentRegion; }
			set
			{
				currentRegion = value;
				NotifyOfPropertyChange("CurrentRegion");
			}
		}

		public bool GroupByProduct
		{
			get { return groupByProduct; }
			set
			{
				groupByProduct = value;
				Offers = Sort(Offers);
				NotifyOfPropertyChange("GroupByProduct");
			}
		}

		public decimal RetailCost
		{
			get
			{
				if (CurrentOffer == null)
					return 0;
				return Math.Round(CurrentOffer.Cost * (1 + RetailMarkup / 100), 2);
			}
		}

		public decimal RetailMarkup
		{
			get
			{
				return retailMarkup == 0 ? MarkupConfig.Calculate(Settings.Markups, CurrentOffer) : retailMarkup;
			}
			set
			{
				retailMarkup = value;
				NotifyOfPropertyChange("RetailMarkup");
			}
		}

		private List<Offer> Sort(List<Offer> offers)
		{
			if (offers == null)
				return null;

			if (GroupByProduct) {
				return SortByMinCostInGroup(offers, o => o.ProductId);
			}
			else {
				return SortByMinCostInGroup(offers, o => o.CatalogId, false);
			}
		}

		public bool CanPrint
		{
			get { return true; }
		}

		public PrintResult Print()
		{
			var doc = new CatalogOfferDocument(Offers, Name);
			return new PrintResult(DisplayName, doc);
		}

		public void ShowPrice()
		{
			if (CurrentOffer == null)
				return;

			var price = CurrentOffer.Price;
			var catalogViewModel = new PriceViewModel {
				CurrentPrice = price
			};
			var offerViewModel = new PriceOfferViewModel(price.Id, catalogViewModel.ShowLeaders);

			//временно не работает пока не придумаю решения по лучше
			//offerViewModel.CurrentOffer = offerViewModel.Offers.FirstOrDefault(o => o.Id == CurrentOffer.Id);

			Shell.NavigateAndReset(catalogViewModel, offerViewModel);
		}

		public void SearchInCatalog(string text)
		{
			if (Shell == null)
				return;

			var catalog = Shell.NavigationStack.LastOrDefault() as CatalogViewModel;
			if (catalog == null)
				return;

			catalog.SearchText = text;
			TryClose();
		}
	}
}
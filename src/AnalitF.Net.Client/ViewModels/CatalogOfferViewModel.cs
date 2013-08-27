using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
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
		private CatalogName filterCatalogName;
		private Catalog filterCatalog;

		private CatalogOfferViewModel()
		{
			NeedToCalculateDiff = true;
			DisplayName = "Сводный прайс-лист";
			Filters = new [] { "Все", "Основные", "Неосновные" };

			MaxProducerCosts = new NotifyValue<List<MaxProducerCost>>();
			CurrentFilter = new NotifyValue<string>(Filters[0]);
			Regions = new NotifyValue<List<string>>();
			CurrentRegion = new NotifyValue<string>(Consts.AllRegionLabel);

			GroupByProduct = new NotifyValue<bool>(true, () => Settings.Value.GroupByProduct, Settings);
			GroupByProduct.Changed().Subscribe(_ => Offers = Sort(Offers));
			RetailMarkup = new NotifyValue<decimal>(true,
				() => MarkupConfig.Calculate(Settings.Value.Markups, CurrentOffer),
				Settings);
			RetailCost = new NotifyValue<decimal?>(() => {
				if (CurrentOffer == null)
					return null;
				return Math.Round(CurrentOffer.Cost * (1 + RetailMarkup / 100), 2);
			}, RetailMarkup);

			CurrentRegion.Changed()
				.Merge(CurrentFilter.Changed())
				.Merge(CurrentProducer.Changed())
				.Subscribe(_ => Update());

			this.ObservableForProperty(m => m.CurrentOffer)
				.Subscribe(_ => {
					RetailCost.Recalculate();
					RetailMarkup.Recalculate();
				});
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

		public bool IsFilterByCatalogName
		{
			get { return filterCatalogName != null; }
		}

		public string[] Filters { get; set; }

		public NotifyValue<string> CurrentFilter { get; set; }
		public NotifyValue<List<MaxProducerCost>> MaxProducerCosts { get; set; }
		public NotifyValue<List<string>> Regions { get; set; }
		public NotifyValue<string> CurrentRegion { get; set; }
		public NotifyValue<bool> GroupByProduct { get; set; }
		public NotifyValue<decimal?> RetailCost { get; set; }
		public NotifyValue<decimal> RetailMarkup { get; set; }

		public bool CanPrint
		{
			get { return true; }
		}

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
			MaxProducerCosts.Value = StatelessSession.Query<MaxProducerCost>()
				.Where(c => c.CatalogId == catalogId)
				.OrderBy(c => c.Product)
				.ThenBy(c => c.Producer)
				.ToList();
		}

		private void UpdateRegions()
		{
			var offerRegions = Offers.Select(o => o.Price.RegionName).Distinct()
				.OrderBy(r => r)
				.ToList();
			Regions.Value = new[] { Consts.AllRegionLabel }.Concat(offerRegions).ToList();
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

			var region = CurrentRegion.Value;
			if (region != Consts.AllRegionLabel) {
				queryable = queryable.Where(o => o.Price.RegionName == region);
			}
			var producer = CurrentProducer.Value;
			if (producer != Consts.AllProducerLabel) {
				queryable = queryable.Where(o => o.Producer == producer);
			}
			var filter = CurrentFilter.Value;
			if (filter == Filters[1]) {
				queryable = queryable.Where(o => o.Price.BasePrice);
			}
			if (filter == Filters[2]) {
				queryable = queryable.Where(o => !o.Price.BasePrice);
			}

			var offers = queryable.Fetch(o => o.Price).ToList();
			offers = Sort(offers);
			if (IsFilterByCatalogName) {
				offers.Each(o => o.GroupName = catalogs.Where(c => c.Id == o.CatalogId)
					.Select(c => c.FullName)
					.FirstOrDefault());
			}
			Offers = offers;
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
				CurrentPrice = {
					Value = price
				}
			};
			var offerViewModel = new PriceOfferViewModel(price.Id, catalogViewModel.ShowLeaders);

			//todo: временно не работает пока не придумаю решения по лучше
			//offerViewModel.CurrentOffer = offerViewModel.Offers.FirstOrDefault(o => o.Id == CurrentOffer.Id);

			Shell.NavigateAndReset(catalogViewModel, offerViewModel);
		}

		public void SearchInCatalog(object sender, TextCompositionEventArgs args)
		{
			var text = args.Text;
			if (Shell == null)
				return;
			var catalog = Shell.NavigationStack.LastOrDefault() as CatalogViewModel;
			if (catalog == null)
				return;
			if (text.All(char.IsControl))
				return;

			args.Handled = true;
			catalog.SearchText = text;
			TryClose();
		}
	}
}
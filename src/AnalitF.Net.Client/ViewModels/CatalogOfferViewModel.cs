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
using NHibernate.Util;
using ReactiveUI;
using System.Reactive.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class CatalogOfferViewModel : BaseOfferViewModel, IPrintable
	{
		private CatalogName filterCatalogName;
		private Catalog filterCatalog;

		private List<Offer> CatalogOffers = new List<Offer>();

		private CatalogOfferViewModel(OfferComposedId initOfferId = null)
			: base(initOfferId)
		{
			NeedToCalculateDiff = true;
			DisplayName = "Сводный прайс-лист";
			Filters = new [] { "Все", "Основные", "Неосновные" };

			MaxProducerCosts = new NotifyValue<List<MaxProducerCost>>();
			CurrentFilter = new NotifyValue<string>(Filters[0]);
			Regions = new NotifyValue<List<string>>();
			CurrentRegion = new NotifyValue<string>(Consts.AllRegionLabel);

			GroupByProduct = new NotifyValue<bool>(true, () => Settings.Value.GroupByProduct, Settings);
			GroupByProduct.Changed().Subscribe(_ => Offers.Value = Sort(Offers.Value));
			RetailMarkup = new NotifyValue<decimal>(true,
				() => MarkupConfig.Calculate(Settings.Value.Markups, CurrentOffer.Value, User),
				Settings);
			RetailCost = new NotifyValue<decimal?>(() => {
				if (CurrentOffer.Value == null)
					return null;
				return Math.Round(CurrentOffer.Value.ResultCost * (1 + RetailMarkup / 100), 2);
			}, RetailMarkup);

			CurrentRegion.Changed()
				.Merge(CurrentFilter.Changed())
				.Merge(CurrentProducer.Changed())
				.Subscribe(_ => Update());

			this.ObservableForProperty(m => m.CurrentOffer.Value)
				.Subscribe(_ => {
					RetailCost.Recalculate();
					RetailMarkup.Recalculate();
				});
		}

		public CatalogOfferViewModel(Catalog catalog, OfferComposedId initOfferId = null)
			: this(initOfferId)
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
			get { return User.CanPrint<CatalogOfferDocument>(); }
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Update();
			UpdateMaxProducers();

			CurrentOffer.Value = CurrentOffer.Value
				?? Offers.Value.FirstOrDefault(o => o.Price.BasePrice)
				?? Offers.Value.FirstOrDefault();
			UpdateFilters();
		}

		protected override void OnActivate()
		{
			base.OnActivate();

			if (CatalogOffers.Count == 0) {
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

		private void UpdateFilters()
		{
			Regions.Value = new[] { Consts.AllRegionLabel }
				.Concat(CatalogOffers.Select(o => o.Price.RegionName).Distinct()
					.OrderBy(r => r))
				.ToList();

			Producers.Value = new[] { Consts.AllRegionLabel }
				.Concat(CatalogOffers.Select(o => o.Producer).Distinct()
					.OrderBy(p => p))
				.ToList();
		}

		protected override void Query()
		{
			if (CatalogOffers.Count == 0) {
				if (IsFilterByCatalogName) {
					var catalogs = StatelessSession.Query<Catalog>()
						.Fetch(c => c.Name)
						.Where(c => c.Name == filterCatalogName).ToArray();
					var ids = catalogs.Select(c => c.Id).ToArray();

					CatalogOffers = StatelessSession.Query<Offer>()
						.Where(o => ids.Contains(o.CatalogId))
						.Fetch(o => o.Price)
						.ToList();
					CatalogOffers.Each(o => o.GroupName = catalogs.Where(c => c.Id == o.CatalogId)
						.Select(c => c.FullName)
						.FirstOrDefault());
				}
				else {
					var catalogId = filterCatalog.Id;
					CatalogOffers = StatelessSession.Query<Offer>().Where(o => o.CatalogId == catalogId)
						.Fetch(o => o.Price)
						.ToList();
				}
			}

			Filter();
		}

		public void Filter()
		{
			var region = CurrentRegion.Value;
			IEnumerable<Offer> offers = CatalogOffers;
			if (region != Consts.AllRegionLabel) {
				offers = offers.Where(o => o.Price.RegionName == region);
			}
			var producer = CurrentProducer.Value;
			if (producer != Consts.AllProducerLabel) {
				offers = offers.Where(o => o.Producer == producer);
			}
			var filter = CurrentFilter.Value;
			if (filter == Filters[1]) {
				offers = offers.Where(o => o.Price.BasePrice);
			}
			if (filter == Filters[2]) {
				offers = offers.Where(o => !o.Price.BasePrice);
			}

			Offers.Value = Sort(offers.ToList());
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
			var doc = new CatalogOfferDocument(Offers.Value, Name);
			return new PrintResult(DisplayName, doc);
		}

		public void ShowPrice()
		{
			if (CurrentOffer.Value == null)
				return;

			var price = CurrentOffer.Value.Price;
			var catalogViewModel = new PriceViewModel {
				CurrentPrice = {
					Value = price
				}
			};
			var offerViewModel = new PriceOfferViewModel(price.Id, catalogViewModel.ShowLeaders, CurrentOffer.Value.Id);
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
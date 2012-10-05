using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class OfferViewModel : BaseOfferViewModel
	{
		private const string allRegionLabel = "Все регионы";

		private string currentRegion;
		private List<string> regions;
		private string currentFilter;
		private bool groupByProduct;

		private decimal retailMarkup;

		private Settings settings;

		public OfferViewModel(Catalog catalog)
		{
			DisplayName = "Сводный прайс-лист";
			CurrentCatalog = catalog;
			Filters = new [] { "Все", "Основные", "Неосновные" };
			CurrentFilter = Filters[0];
			CurrentRegion = allRegionLabel;
			CurrentProducer = AllProducerLabel;
			settings = Session.Query<Settings>().First();

			this.ObservableForProperty(m => m.CurrentRegion)
				.Merge(this.ObservableForProperty(m => m.CurrentProducer))
				.Merge(this.ObservableForProperty(m => m.CurrentFilter))
				.Subscribe(e => Filter());

			this.ObservableForProperty(m => m.CurrentOffer)
				.Subscribe(_ => RaisePropertyChangedEventImmediately("RetailMarkup"));

			this.ObservableForProperty(m => m.RetailMarkup)
				.Subscribe(_ => RaisePropertyChangedEventImmediately("RetailCost"));

			this.ObservableForProperty(m => m.CurrentOffer)
				.Subscribe(_ => RaisePropertyChangedEventImmediately("Price"));

			Filter();

			CurrentOffer = Offers.FirstOrDefault(o => o.Price.BasePrice);
			if (CurrentOffer == null)
				CurrentOffer = offers.FirstOrDefault();

			UpdateRegions();
			UpdateProducers();
		}

		private void UpdateRegions()
		{
			var offerRegions = Offers.Select(o => o.RegionName).Distinct().OrderBy(r => r).ToList();
			Regions = new[] { allRegionLabel }.Concat(offerRegions).ToList();
		}

		private void Filter()
		{
			var queryable = Session.Query<Offer>().Where(o => o.CatalogId == CurrentCatalog.Id);
			if (CurrentRegion != allRegionLabel) {
				queryable = queryable.Where(o => o.RegionName == CurrentRegion);
			}
			if (CurrentProducer != AllProducerLabel) {
				queryable = queryable.Where(o => o.ProducerSynonym == CurrentProducer);
			}
			if (CurrentFilter == Filters[1]) {
				queryable = queryable.Where(o => o.Price.BasePrice);
			}
			if (CurrentFilter == Filters[2]) {
				queryable = queryable.Where(o => !o.Price.BasePrice);
			}

			var offers = queryable.ToList();
			offers = Sort(offers);
			Offers = offers;
			Calculate();
		}

		private void Calculate()
		{
			CalculateDiff();

			CalculateRetailCost();
		}

		private void CalculateDiff()
		{
			decimal baseCost = 0;
			if (settings.DiffCalcMode == DiffCalcMode.MinCost)
				baseCost = Offers.Select(o => o.Cost).DefaultIfEmpty().Min();
			else if (settings.DiffCalcMode == DiffCalcMode.MinBaseCost)
				baseCost = Offers.Where(o => o.Price.BasePrice).Select(o => o.Cost).DefaultIfEmpty().Min();

			foreach (var offer in Offers) {
				offer.CalculateDiff(baseCost);
				if (settings.DiffCalcMode == DiffCalcMode.PrevOffer)
					baseCost = offer.Cost;
			}
		}

		public string[] Filters { get; set; }

		public Price Price
		{
			get
			{
				if (CurrentOffer == null)
					return null;
				return Session.Load<Price>(CurrentOffer.Price.Id);
			}
		}

		public string CurrentFilter
		{
			get { return currentFilter; }
			set
			{
				currentFilter = value;
				RaisePropertyChangedEventImmediately("CurrentFilter");
			}
		}

		public List<string> Regions
		{
			get { return regions; }
			set
			{
				regions = value;
				RaisePropertyChangedEventImmediately("Regions");
			}
		}

		public string CurrentRegion
		{
			get { return currentRegion; }
			set
			{
				currentRegion = value;
				RaisePropertyChangedEventImmediately("CurrentRegion");
			}
		}

		public bool GroupByProduct
		{
			get { return groupByProduct; }
			set
			{
				groupByProduct = value;
				Offers = Sort(Offers);
				RaisePropertyChangedEventImmediately("GroupByProduct");
			}
		}

		public decimal RetailCost
		{
			get
			{
				if (CurrentOffer == null)
					return 0;
				return CurrentOffer.Cost * (1 + RetailMarkup / 100);
			}
		}

		public decimal RetailMarkup
		{
			get
			{
				return retailMarkup == 0 ? MarkupConfig.Calculate(markups, CurrentOffer) : retailMarkup;
			}
			set
			{
				retailMarkup = value;
				RaisePropertyChangedEventImmediately("RetailMarkup");
			}
		}

		private List<Offer> Sort(List<Offer> offer)
		{
			if (GroupByProduct) {
				return SortByMinCostInGroup(offer, o => o.ProductId);
			}
			return offer.OrderBy(o => o.Cost).ToList();
		}
	}
}
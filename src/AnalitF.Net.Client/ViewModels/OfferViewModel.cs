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
		private string currentRegion;
		private List<string> regions;
		private string currentFilter;
		private bool groupByProduct;

		private const string allRegionLabel = "Все регионы";

		public OfferViewModel(Catalog catalog)
		{
			DisplayName = "Сводный прайс-лист";
			CurrentCatalog = catalog;
			Filters = new [] { "Все", "Основные", "Неосновные" };
			CurrentFilter = Filters[0];
			CurrentRegion = allRegionLabel;
			CurrentProducer = AllProducerLabel;

			this.ObservableForProperty(m => m.CurrentRegion)
				.Merge(this.ObservableForProperty(m => m.CurrentProducer))
				.Merge(this.ObservableForProperty(m => m.CurrentFilter))
				.Subscribe(e => Filter());
			Filter();

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
			Offers = Sort(queryable.ToList());
		}

		public string[] Filters { get; set; }

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

		private List<Offer> Sort(List<Offer> offer)
		{
			if (GroupByProduct) {
				var lookup = offer.GroupBy(o => o.ProductId).ToDictionary(g => g.Key, g => g.Min(o => o.Cost));
				return offer.OrderBy(o => Tuple.Create(lookup[o.ProductId], o.Cost)).ToList();
			}
			return offer.OrderBy(o => o.Cost).ToList();
		}
	}
}
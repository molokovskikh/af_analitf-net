using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Models;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class SearchOfferViewModel : BaseOfferViewModel
	{
		private string searchText;
		private Price currentPrice;
		private string activeSearchTerm;
		private bool onlyBase;

		public SearchOfferViewModel()
		{
			DisplayName = "Поиск в прайс-листах";
			NeedToCalculateDiff = true;
			NavigateOnShowCatalog = true;

			var producers = StatelessSession.Query<Offer>().Select(o => o.Producer).ToList().Distinct().OrderBy(p => p);
			Producers = new[] { Consts.AllProducerLabel }.Concat(producers).ToList();
			CurrentProducer = Consts.AllProducerLabel;

			var prices = Session.Query<Price>().OrderBy(p => p.Name);
			Prices = new[] { new Price {Name = Consts.AllPricesLabel} }.Concat(prices).ToList();
			CurrentPrice = Prices.First();

			this.ObservableForProperty(m => m.SearchText)
				.Throttle(Consts.SearchTimeout, Scheduler)
				.ObserveOn(UiScheduler)
				.Subscribe(_ => Search());

			this.ObservableForProperty(m => (object)m.CurrentPrice)
				.Merge(this.ObservableForProperty(m => (object)m.CurrentProducer))
				.Merge(this.ObservableForProperty(m => (object)m.OnlyBase))
				.Subscribe(_ => Update());
		}

		public void Search()
		{
			if (string.IsNullOrEmpty(SearchText) || SearchText.Length < 3) {
				return;
			}

			ActiveSearchTerm = SearchText;
			SearchText = "";
			Update();
		}

		public void ClearSearch()
		{
			ActiveSearchTerm = "";
			SearchText = "";
			CurrentOffer = null;
			Offers = new List<Offer>();
		}

		public string SearchText
		{
			get { return searchText; }
			set
			{
				searchText = value;
				NotifyOfPropertyChange("SearchText");
			}
		}

		public string ActiveSearchTerm
		{
			get { return activeSearchTerm; }
			set
			{
				activeSearchTerm = value;
				NotifyOfPropertyChange("ActiveSearchTerm");
			}
		}

		public List<Price> Prices { get; set; }

		public Price CurrentPrice
		{
			get { return currentPrice; }
			set
			{
				currentPrice = value;
				NotifyOfPropertyChange("CurrentPrice");
			}
		}

		public bool OnlyBase
		{
			get
			{
				return onlyBase;
			}
			set
			{
				onlyBase = value;
				NotifyOfPropertyChange("OnlyBase");
			}
		}

		protected override void Query()
		{
			if (String.IsNullOrEmpty(ActiveSearchTerm))
				return;

			var query = StatelessSession.Query<Offer>().Where(o => o.ProductSynonym.Contains(ActiveSearchTerm));
			if (currentPrice != null && currentPrice.Id != null) {
				query = query.Where(o => o.Price.Id == currentPrice.Id);
			}

			if (CurrentProducer != null && CurrentProducer != Consts.AllProducerLabel) {
				query = query.Where(o => o.Producer == CurrentProducer);
			}

			if (OnlyBase) {
				query = query.Where(o => o.Price.BasePrice);
			}

			var offer = query.Fetch(o => o.Price).ToList();
			if (Settings.GroupByProduct) {
				Offers = SortByMinCostInGroup(offer, o => o.ProductId);
			}
			else {
				Offers = SortByMinCostInGroup(offer, o => o.CatalogId);
			}
		}
	}
}
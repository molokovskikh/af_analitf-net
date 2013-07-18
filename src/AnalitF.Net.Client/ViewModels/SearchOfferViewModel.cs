using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class SearchOfferViewModel : BaseOfferViewModel
	{
		private string searchText;
		private string activeSearchTerm;

		public SearchOfferViewModel()
		{
			DisplayName = "Поиск в прайс-листах";
			NeedToCalculateDiff = true;
			NavigateOnShowCatalog = true;

			OnlyBase = new NotifyValue<bool>();

			var producers = StatelessSession.Query<Offer>()
				.Select(o => o.Producer)
				.Distinct()
				.ToList()
				.OrderBy(p => p)
				.ToList();
			producers = new[] { Consts.AllProducerLabel }.Concat(producers).ToList();
			Producers = new NotifyValue<List<string>>(producers);

			var prices = Session.Query<Price>().OrderBy(p => p.Name);
			Prices = new[] { new Price {Name = Consts.AllPricesLabel} }.Concat(prices).ToList();
			CurrentPrice = new NotifyValue<Price>(Prices.First());
			Settings.Changed().Subscribe(_ => SortOffers(Offers));

			this.ObservableForProperty(m => m.SearchText)
				.Throttle(Consts.SearchTimeout, Scheduler)
				.ObserveOn(UiScheduler)
				.Subscribe(_ => Search());

			CurrentPrice.Changed()
				.Merge(OnlyBase.Changed())
				.Merge(CurrentProducer.Changed())
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

		public NotifyValue<Price> CurrentPrice { get; set; }

		public NotifyValue<bool> OnlyBase { get; set; }

		protected override void Query()
		{
			if (String.IsNullOrEmpty(ActiveSearchTerm))
				return;

			var query = StatelessSession.Query<Offer>().Where(o => o.ProductSynonym.Contains(ActiveSearchTerm));
			var price = CurrentPrice.Value;
			if (price != null && price.Id != null) {
				var priceId = price.Id;
				query = query.Where(o => o.Price.Id == priceId);
			}

			var producer = CurrentProducer.Value;
			if (producer != Consts.AllProducerLabel)
				query = query.Where(o => o.Producer == producer);

			if (OnlyBase)
				query = query.Where(o => o.Price.BasePrice);

			SortOffers(query.Fetch(o => o.Price).ToList());
		}

		private void SortOffers(List<Offer> result)
		{
			if (Settings.Value.GroupByProduct) {
				Offers = SortByMinCostInGroup(result, o => o.ProductId);
			}
			else {
				Offers = SortByMinCostInGroup(result, o => o.CatalogId);
			}
		}
	}
}
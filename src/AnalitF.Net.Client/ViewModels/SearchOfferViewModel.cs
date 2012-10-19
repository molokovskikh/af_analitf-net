using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class SearchOfferViewModel : BaseOfferViewModel
	{
		private string searchText;
		private Price currentPrice;

		public SearchOfferViewModel()
		{
			DisplayName = "Поиск в прайс-листах";
			NeedToCalculateDiff = true;

			var producers = Session.Query<Offer>().Select(o => o.ProducerSynonym).ToList().Distinct().OrderBy(p => p);
			Producers = new[] { AllProducerLabel }.Concat(producers).ToList();
			CurrentProducer = AllProducerLabel;

			var prices = Session.Query<Price>().OrderBy(p => p.Name);
			Prices = new[] { new Price {Name = Consts.AllPricesLabel} }.Concat(prices).ToList();
			CurrentPrice = Prices.First();
		}

		public void Search()
		{
			if (String.IsNullOrEmpty(SearchText))
				return;

			var query = Session.Query<Offer>().Where(o => o.ProductSynonym.Contains(SearchText));
			if (currentPrice != null && currentPrice.Id > 0) {
				query = query.Where(o => o.Price.Id == currentPrice.Id);
			}

			if (CurrentProducer != null && CurrentProducer != AllProducerLabel) {
				query = query.Where(o => o.ProducerSynonym == CurrentProducer);
			}

			if (Settings.GroupByProduct) {
				Offers = SortByMinCostInGroup(query.ToList(), o => o.ProductId);
			}
			else {
				Offers = SortByMinCostInGroup(query.ToList(), o => o.CatalogId);
			}

			Calculate();
		}

		public string SearchText
		{
			get { return searchText; }
			set
			{
				searchText = value;
				RaisePropertyChangedEventImmediately("SearchText");
			}
		}

		public List<Price> Prices { get; set; }

		public Price CurrentPrice
		{
			get { return currentPrice; }
			set
			{
				currentPrice = value;
				RaisePropertyChangedEventImmediately("CurrentPrice");
			}
		}
	}
}
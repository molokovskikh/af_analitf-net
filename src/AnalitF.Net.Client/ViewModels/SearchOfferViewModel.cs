﻿using System;
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

			var producers = StatelessSession.Query<Offer>().Select(o => o.Producer).ToList().Distinct().OrderBy(p => p);
			Producers = new[] { Consts.AllProducerLabel }.Concat(producers).ToList();
			CurrentProducer = Consts.AllProducerLabel;

			var prices = Session.Query<Price>().OrderBy(p => p.Name);
			Prices = new[] { new Price {Name = Consts.AllPricesLabel} }.Concat(prices).ToList();
			CurrentPrice = Prices.First();
		}

		public void Search()
		{
			Update();
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

		protected override void Query()
		{
			if (String.IsNullOrEmpty(SearchText))
				return;

			var query = StatelessSession.Query<Offer>().Where(o => o.ProductSynonym.Contains(SearchText));
			if (currentPrice != null && currentPrice.Id != null) {
				query = query.Where(o => o.Price.Id == currentPrice.Id);
			}

			if (CurrentProducer != null && CurrentProducer != Consts.AllProducerLabel) {
				query = query.Where(o => o.Producer == CurrentProducer);
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
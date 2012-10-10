using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class PriceOfferViewModel : BaseOfferViewModel
	{
		private string[] filters = new[] {
			"Прайс-лист (F4)",
			"Заказы (F5)",
			"Лучшие предложения (F6)",
		};

		private string currentFilter;
		private string filterText;

		public PriceOfferViewModel(Price price, bool showLeaders)
		{
			DisplayName = "Заявка поставщику";
			Price = price;

			Filters = filters;
			currentProducer = AllProducerLabel;
			currentFilter = filters[0];
			if (showLeaders)
				currentFilter = filters[2];

			this.ObservableForProperty(m => m.CurrentFilter)
				.Merge(this.ObservableForProperty(m => m.CurrentProducer))
				.Subscribe(e => Update());

			Update();
			UpdateProducers();
		}

		public Price Price { get; set; }

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

		private void Update()
		{
			var query = Session.Query<Offer>().Where(o => o.Price.Id == Price.Id);
			if (CurrentProducer != AllProducerLabel) {
				query = query.Where(o => o.ProducerSynonym == CurrentProducer);
			}
			if (CurrentFilter == filters[2]) {
				query = query.Where(o => o.LeaderPrice == Price);
			}
			if (currentFilter == filters[1]) {
				query = query.Where(o => o.OrderLine != null);
			}
			if (!String.IsNullOrEmpty(filterText)) {
				query = query.Where(o => o.ProductSynonym.Contains(filterText));
			}

			Offers = query.ToList();
			CurrentOffer = offers.FirstOrDefault();
		}

		public string SearchText { get; set; }

		public void Search()
		{
			if (string.IsNullOrEmpty(SearchText))
				return;

			filterText = SearchText;
			Update();
		}
	}
}
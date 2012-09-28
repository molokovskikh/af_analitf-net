using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using NHibernate.Linq;

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

		public PriceOfferViewModel(Price price, bool showLeaders)
		{
			DisplayName = "Заявка поставщику";
			Price = price;

			Filters = filters;
			currentProducer = AllProducerLabel;
			currentFilter = filters[0];
			if (showLeaders)
				currentFilter = filters[2];

			Filter();
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
				Filter();
			}
		}

		private void Filter()
		{
			var query = Session.Query<Offer>().Where(o => o.PriceId == Price.Id);
			if (CurrentProducer != AllProducerLabel) {
				query = query.Where(o => o.ProducerSynonym == CurrentProducer);
			}
			if (CurrentFilter == filters[2]) {
				query = query.Where(o => o.LeaderPrice == Price);
			}
			if (currentFilter == filters[1]) {
				query = query.Where(o => o.Line != null);
			}
			Offers = query.ToList();
			CurrentOffer = offers.FirstOrDefault();
		}
	}
}
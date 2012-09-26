using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class PriceOfferViewModel : BaseScreen
	{
		private Catalog currentCatalog;
		private Offer currentOffer;
		private List<string> producers;
		private string currentProducer;
		private List<Offer> offers;
		private string allLabel = "Все производители";
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
			currentProducer = allLabel;
			currentFilter = filters[0];
			if (showLeaders)
				currentFilter = filters[2];

			Filter();
		}

		public Price Price { get; set; }

		public List<Offer> Offers
		{
			get { return offers; }
			set
			{
				offers = value;
				RaisePropertyChangedEventImmediately("Offers");
			}
		}

		public List<string> Producers
		{
			get { return producers; }
			set
			{
				producers = value;
				RaisePropertyChangedEventImmediately("Producers");
			}
		}

		public string CurrentProducer
		{
			get { return currentProducer; }
			set
			{
				currentProducer = value;
				RaisePropertyChangedEventImmediately("CurrentProducer");
				Filter();
			}
		}

		public Offer CurrentOffer
		{
			get { return currentOffer; }
			set
			{
				currentOffer = value;
				RaisePropertyChangedEventImmediately("CurrentOffer");
				CurrentCatalog = Session.Load<Catalog>(currentOffer.CatalogId);
			}
		}

		public Catalog CurrentCatalog
		{
			get { return currentCatalog; }
			set
			{
				currentCatalog = value;
				RaisePropertyChangedEventImmediately("CurrentCatalog");
				RaisePropertyChangedEventImmediately("CanShowDescription");
			}
		}

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
			if (CurrentProducer != allLabel) {
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
			Producers = new[] { allLabel }.Concat(Offers.Select(o => o.ProducerSynonym).ToList()).ToList();
		}

		public bool CanShowDescription
		{
			get
			{
				return CurrentOffer != null
					&& CurrentCatalog != null
					&& CurrentCatalog.Name.Description != null;
			}
		}

		public void ShowDescription()
		{
			if (!CanShowDescription)
				return;

			Manager.ShowDialog(new DescriptionViewModel(CurrentCatalog.Name.Description));
		}
	}
}
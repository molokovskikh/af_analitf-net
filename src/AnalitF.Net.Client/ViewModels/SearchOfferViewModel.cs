using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class SearchOfferViewModel : BaseOfferViewModel
	{
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

			CurrentPrice.Changed()
				.Merge(OnlyBase.Changed())
				.Merge(CurrentProducer.Changed())
				.Subscribe(_ => Update());
			SearchBehavior = new SearchBehavior(OnCloseDisposable, this);
		}

		public SearchBehavior SearchBehavior { get; set; }

		public IResult Search()
		{
			return SearchBehavior.Search();
		}

		public IResult ClearSearch()
		{
			Offers.Value = new List<Offer>();
			return SearchBehavior.ClearSearch();
		}

		public List<Price> Prices { get; set; }

		public NotifyValue<Price> CurrentPrice { get; set; }

		public NotifyValue<bool> OnlyBase { get; set; }

		protected override void Query()
		{
			var term = SearchBehavior.ActiveSearchTerm.Value;
			if (String.IsNullOrEmpty(term))
				return;

			var query = StatelessSession.Query<Offer>().Where(o => o.ProductSynonym.Contains(term));
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
				Offers.Value = SortByMinCostInGroup(result, o => o.ProductId);
			}
			else {
				Offers.Value = SortByMinCostInGroup(result, o => o.CatalogId);
			}
		}
	}
}
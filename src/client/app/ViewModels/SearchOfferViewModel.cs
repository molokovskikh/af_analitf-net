using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using NHibernate.Criterion;
using NHibernate.Linq;
using ReactiveUI;
using Ex=System.Linq.Expressions.Expression;

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

			Producers.Value = new[] { EmptyProducer }
				.Concat(StatelessSession.Query<Producer>()
					.OrderBy(p => p.Name)
					.ToList())
				.ToList();

			var prices = Session.Query<Price>().OrderBy(p => p.Name);
			Prices = prices.Select(p => new Selectable<Price>(p)).ToList();
			Settings.Changed().Subscribe(_ => SortOffers(Offers));

			Prices.Select(p => p.Changed()).Merge().Throttle(Consts.FilterUpdateTimeout, UiScheduler)
				.Merge(OnlyBase.Changed())
				.Merge(CurrentProducer.Changed())
				.Subscribe(_ => Update());
			SearchBehavior = new SearchBehavior(this);
		}

		public SearchBehavior SearchBehavior { get; set; }
		public List<Selectable<Price>> Prices { get; set; }
		public NotifyValue<bool> OnlyBase { get; set; }

		public IResult Search()
		{
			return SearchBehavior.Search();
		}

		public IResult ClearSearch()
		{
			Offers.Value = new List<Offer>();
			return SearchBehavior.ClearSearch();
		}

		protected override void Query()
		{
			var term = SearchBehavior.ActiveSearchTerm.Value;
			if (String.IsNullOrEmpty(term))
				return;

			var query = StatelessSession.Query<Offer>().Where(o => o.ProductSynonym.Contains(term));
			query = Util.Filter(query, o => o.Price.Id, Prices);

			var producer = CurrentProducer.Value;
			if (producer != null && producer.Id > 0) {
				var id = producer.Id;
				query = query.Where(o => o.ProducerId == id);
			}

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
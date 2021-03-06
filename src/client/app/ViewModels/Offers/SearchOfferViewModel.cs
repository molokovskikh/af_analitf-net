﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Offers
{
	public class SearchOfferViewModel : BaseOfferViewModel
	{
		private string initTerm;

		public SearchOfferViewModel(string term)
			: this()
		{
			initTerm = term;
		}

		public SearchOfferViewModel()
		{
			DisplayName = "Поиск в прайс-листах";
			NeedToCalculateDiff = true;
			NavigateOnShowCatalog = true;

			HideJunk = new NotifyValue<bool>();
			OnlyBase = new NotifyValue<bool>();

			if (Session != null) {
				Prices = Session.Query<Price>()
					.OrderBy(p => p.Name)
					.Select(p => new Selectable<Price>(p))
					.ToList();
			}
			else {
				Prices = new List<Selectable<Price>>();
			}
			Settings.Subscribe(_ => {
				Offers.Value = SortOffers(Offers.Value);
			});
			SearchBehavior = new SearchBehavior(this);
			IsLoading = new NotifyValue<bool>();
			Persist(HideJunk, "HideJunk");
		}

		public SearchBehavior SearchBehavior { get; set; }
		public List<Selectable<Price>> Prices { get; set; }
		public NotifyValue<bool> OnlyBase { get; set; }
		public NotifyValue<bool> HideJunk { get; set; }
		public NotifyValue<bool> IsLoading { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			var obProducers = DbReloadToken
				.SelectMany(_ => RxQuery(s => new[] {EmptyProducer}
					.Concat(s.Query<Producer>()
						.OrderBy(p => p.Name)
						.ToList())
					.ToList()));
			obProducers.Subscribe(Producers);
			obProducers.Subscribe(_ =>
			{
				var currentFilterProducerId = CurrentFilterProducer.HasValue ? CurrentFilterProducer.Value.Id : 0;
				if (CanSaveFilterProducer.Value && currentFilterProducerId > 0 &&
					  Producers.Value.Any(d => d.Id == currentFilterProducerId))
				{
					CurrentProducer.Value = CurrentFilterProducer.Value;
				}
			});

			SearchBehavior.ActiveSearchTerm.Cast<object>()
				.Merge(Prices.Select(p => p.Changed()).Merge().Throttle(Consts.FilterUpdateTimeout, UiScheduler))
				.Merge(OnlyBase.Changed())
				.Merge(CurrentProducer.Changed())
				.Merge(HideJunk.Changed())
				.Merge(DbReloadToken)
				.Select(v => {
					ProducerFilterStateSet();

					var term = SearchBehavior.ActiveSearchTerm.Value;
					if (String.IsNullOrEmpty(term))
						return Observable.Return(new List<Offer>());
					IsLoading.Value = true;
					return RxQuery(s => {
						var query = s.Query<Offer>();
						query = Util.ContainsAny(query, o => o.ProductSynonym,
							term.Split(new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
						query = Util.Filter(query, o => o.Price.Id, Prices);

						var producer = CurrentProducer.Value;
						if (producer != null && producer.Id > 0) {
							var id = producer.Id;
							query = query.Where(o => o.ProducerId == id);
						}

						if (OnlyBase)
							query = query.Where(o => o.Price.BasePrice);
						if (HideJunk)
							query = query.Where(o => !o.Junk);

						var result = SortOffers(query.Fetch(o => o.Price).ToList());
						return result;
					});
				})
				.Switch()
				//будь бдителен CalculateRetailCost и LoadOrderItems может вызвать обращение к базе если данные еще не загружены
				//тк синхронизация не производится загрузка должна выполняться в основной нитке
				.Do(v => {
					LoadOrderItems(v);
					Calculate(v);
					IsLoading.Value = false;
				})
				.Subscribe(Offers, CloseCancellation.Token);

			//используется в случае если нужно найти предложения по позиции отказа
			if (!String.IsNullOrEmpty(initTerm)) {
				DbReloadToken
					.Do(_ => IsLoading.Value = true)
					.SelectMany(_ => RxQuery(s => QueryByFullText(s, initTerm)))
					.Do(v => {
						LoadOrderItems(v);
						Calculate(v);
						IsLoading.Value = false;
					})
					.Subscribe(Offers, CloseCancellation.Token);
			}
		}

		public static IList<Offer> QueryByFullText(IStatelessSession statelessSession, string term)
		{
			return statelessSession
				.CreateSQLQuery(@"
select {o.*}, {p.*}, match (productsynonym) against (:term in natural language mode)
from Offers o
	join Prices p on p.PriceId = o.PriceId and p.RegionId = o.RegionId
where match (ProductSynonym) against (:term in natural language mode)")
				.AddEntity("o", typeof(Offer))
				.AddJoin("p", "o.Price")
				.SetParameter("term", term)
				.List<Offer>();
		}

		private IList<Offer> SortOffers(IList<Offer> offers)
		{
			if (Settings.Value.GroupByProduct) {
				return SortByMinCostInGroup(offers, o => o.ProductId);
			}
			else {
				return SortByMinCostInGroup(offers, o => o.CatalogId);
			}
		}
	}
}